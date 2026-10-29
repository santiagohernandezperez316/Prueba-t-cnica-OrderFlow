# OrderFlow

Una tienda recibe pedidos y cada pedido reserva inventario antes de confirmarse. Backend en .NET 10
(Orders API + Inventory Worker, MassTransit sobre RabbitMQ, SQL Server) en `backend/` y panel web
en React + TypeScript en `frontend/`.

## Cómo correr todo

Requisito: Docker con Docker Compose.

```bash
docker compose up --build
```

Panel en **http://localhost:8082**. No hace falta `.env` ni ningún paso previo: cada servicio
.NET aplica sus migraciones y siembra datos al arrancar (SKU-001 = 50, SKU-002 = 30,
SKU-003 = 10 unidades).

| Servicio | Puerto |
|---|---|
| Panel (nginx sirve la SPA y hace de proxy a los servicios) | 8082 |
| Orders API (Swagger en `/swagger`) | 8080 |
| Inventory Worker (`GET /products` y `/health/*`) | 8081 |
| RabbitMQ (consola en 15672, `orderflow` / `orderflow_dev`) | 5672 / 15672 |

- El primer arranque descarga SQL Server (~1.5 GB). La imagen de SQL Server 2022 es solo
  amd64: en Apple Silicon corre emulada por Docker Desktop.
- Para empezar desde cero: `docker compose down -v`.
- Configuración por variables de entorno. `docker-compose.yml` trae valores de desarrollo por
  defecto; para cambiarlos, copiar `.env.example` a `.env`. El código no tiene cadenas de conexión
  ni credenciales: si falta una variable, el servicio no arranca y el error nombra la clave.

```bash
curl -i -X POST http://localhost:8080/orders -H "Content-Type: application/json" \
  -d '{"clienteNombre":"Ana","sku":"SKU-001","cantidad":2}'
```

Endpoints: `POST /orders` (201 o 400 con un error por campo), `GET /orders`, `GET /orders/{id}`
(404 si no existe) y `GET /skus` (catálogo para el formulario) en Orders API; `GET /products`
(stock disponible, solo lectura) en Inventory Worker.

### Tests

Solo requiere Docker (no hace falta SDK de .NET ni Node):

```bash
docker compose run --rm --build frontend-tests && docker compose run --rm --build backend-tests
```

Devuelve un código distinto de 0 si algún test falla. En Windows PowerShell 5.1, que no tiene `&&`,
se corren los dos comandos por separado. Los servicios de test tienen `profiles: ["test"]`, así que
`docker compose up` no los levanta. `backend-tests` monta el socket de Docker porque Testcontainers
crea su propio SQL Server.

Alternativa local: `cd backend && dotnet test` (requiere SDK de .NET 10 y Docker) y
`cd frontend && npm ci && npm test` (requiere Node 22.22+ o 24.15+). El backend usa .NET 10 porque
es la LTS actual: .NET 8 sale de soporte en noviembre de 2026.

| Test backend | Qué demuestra |
|---|---|
| `CreateOrder_InvalidFields_ReturnsValidationProblemWithEveryField` | `400` con errores en `clienteNombre`, `sku` y `cantidad` en la misma respuesta |
| `CreateOrder_ValidRequest_SavesPendingOrderAndPublishesOrderCreated` | El pedido queda en `Pending` y `OrderCreated` sale por el outbox |
| `ConfirmOrReject_FromEachStatus_AppliesOnlyFromPending` | Las 6 combinaciones estado × transición: solo actúan desde `Pending` |
| `ReserveStock_SameEventIdWithNewMessageId_DecrementsStockOnce` | El mismo `EventId` con otro `MessageId` (que el inbox no detecta) descuenta stock una sola vez |
| `ReserveStock_ConcurrentOrdersExceedStock_NeverOversells` | 60 reservas concurrentes sobre stock 48, más dos eventos con cantidad 0 y -5: 48 reservadas, el resto rechazadas, stock final 0 |

Los tests levantan cada servicio con `WebApplicationFactory`, SQL Server en Testcontainers y el
test harness de MassTransit en lugar de RabbitMQ.

## Decisiones de arquitectura

```
POST /orders ──► Orders API ──(OrderCreated)──► RabbitMQ ──► Inventory Worker
                     ▲                                             │
                     └────(StockReserved / StockRejected)──────────┘
```

### Mensajería y consistencia

- **RabbitMQ con MassTransit.** MassTransit crea la topología (un exchange por tipo de evento y
  una cola por consumidor) y trae reintentos, colas `_error` y outbox/inbox sobre EF Core. Con
  `RabbitMQ.Client` directo habría que escribir todo eso a mano, que es justo donde están los
  errores sutiles.
- **Outbox al crear el pedido.** El pedido y `OrderCreated` se guardan en el mismo `SaveChanges`
  (`orders.OutboxMessage`); MassTransit publica después del commit. No hay ventana entre guardar
  y publicar.
- **Outbox e inbox en Inventory.** Cada `OrderCreated` se procesa en una transacción de su base:
  `ProcessedEvents`, el descuento y `StockReserved`/`StockRejected` se confirman juntos.
- **Idempotencia del worker.** Antes de descontar, `ReserveStockHandler` registra el `EventId` en
  `inventory.ProcessedEvents` con un `INSERT … WHERE NOT EXISTS (… WITH (UPDLOCK, HOLDLOCK))`. Si
  inserta 0 filas, el evento ya se procesó y termina sin descontar ni publicar. El inbox de
  MassTransit cubre las reentregas del broker (mismo `MessageId`) durante su ventana de 30 minutos;
  `ProcessedEvents` cubre el mismo evento republicado con otro `MessageId` y no expira.
- **Sin sobreventa.** El descuento es `UPDATE … SET Stock = Stock - @cantidad WHERE Sku = @sku AND
  Stock >= @cantidad`. Reservado o rechazado depende de las filas afectadas, y un `CHECK (Stock >= 0)`
  queda como red de seguridad. El worker no confía en el evento: una cantidad ≤ 0 se rechaza sin
  tocar el stock.
- **Transiciones.** `Order.Confirm()` y `Order.Reject()` solo actúan desde `Pending`. Un
  `StockReserved` o `StockRejected` duplicado o tardío se ignora con un warning; por eso los
  consumidores de Orders no necesitan inbox.
- **Push después del commit.** `ConfirmOrder`/`RejectOrder` notifican por SignalR después de su
  `SaveChanges`, así que nunca se anuncia un estado que no quedó guardado.

### Manejo de fallos

- **Broker caído al crear un pedido:** `POST /orders` responde `201` igual y el pedido queda en
  `Pending`; `OrderCreated` espera en el outbox y se publica cuando RabbitMQ vuelve.
- **Inventory Worker caído:** `OrderCreated` espera en la cola durable y el pedido sigue en
  `Pending` hasta que el worker vuelve y lo procesa.
- **Mensaje que falla:** 3 reintentos (500 ms, 2 s, 5 s); después va a `<cola>_error`, visible en
  la consola de RabbitMQ. En Inventory cada reintento usa una transacción nueva: si falla después
  de registrar el `EventId` y descontar, ambos se revierten, y al devolver el mensaje desde `_error`
  se procesa una sola vez.
- **Push de SignalR que falla:** se registra un warning y el mensaje se da por procesado; el
  pedido ya está guardado y el panel lo muestra al recargar la lista (al reconectar el hub o al
  volver a la página).
- **Health checks** (ambos servicios):

  | | `/health/live` | `/health/ready` |
  |---|---|---|
  | Qué revisa | Solo que el proceso responde | Base de datos y bus |
  | Base de datos caída | 200 | 503 |
  | Orders API con RabbitMQ caído | 200 | `Degraded` (200): sigue aceptando pedidos, que esperan en el outbox |
  | Inventory Worker con RabbitMQ caído | 200 | `Degraded` (200) si ya estaba conectado; `Unhealthy` (503) si arranca sin broker, porque sin él no tiene trabajo |

### Datos

SQL Server, con una base por servicio (`OrdersDb`, `InventoryDb`) y migraciones propias. Hacen
falta transacciones que abarquen el dato y los mensajes del outbox, y MassTransit trae outbox e
inbox con EF Core para SQL Server.

### Backend

**Vertical slice en lugar de capas.** Cada caso de uso es un archivo en `Features/` con todo lo
que necesita: endpoint o consumidor, comando, validador y handler. Cambiar "crear pedido" es abrir
`CreateOrder.cs`. Con capas (Domain / Application / Infrastructure / Api) el mismo cambio toca
cuatro proyectos y exige interfaces con una sola implementación; para dos servicios con cuatro o
cinco casos de uso cada uno, eso es ceremonia. Lo que sí se separa es lo que no pertenece a un
caso de uso:

- `Domain/Order.cs`: las transiciones de estado, la única regla de negocio de Orders. No depende
  de EF Core ni de MassTransit, así que se testea sin infraestructura (`OrderTests`).
- `Persistence/`, `Messaging/`, `Realtime/` y `Common/`: configuración técnica compartida por los
  slices (DbContext y migraciones, bus, hub de SignalR, validación y errores).

Estructura:

- `OrderFlow.Orders.Api/Features`: `CreateOrder`, `GetOrders`, `GetOrderById`, `GetSkus`,
  `ConfirmOrder` y `RejectOrder` (estos dos son consumidores de `StockReserved`/`StockRejected`).
- `OrderFlow.Inventory.Worker/Features`: `ReserveStock` (consumidor de `OrderCreated`) y
  `GetProducts`.

**Un proyecto por servicio, contratos compartidos.** Orders API e Inventory Worker no se
referencian entre sí; solo comparten `OrderFlow.Contracts`, con los tres eventos como `record`.
MassTransit enruta por el nombre completo del tipo, así que compartir el ensamblado evita que los
dos lados diverjan. El costo es que un cambio de contrato obliga a desplegar ambos, aceptable en
un monorepo.

**Minimal APIs.** Los endpoints son de pocas líneas y `TypedResults` documenta en OpenAPI las
respuestas posibles (`201`, `400`, `404`); controllers no aportarían nada aquí. El worker también
es una `WebApplication` porque necesita HTTP para los health checks y para `GET /products`.

**MediatR solo para comandos.** `CreateOrder`, `ConfirmOrder`, `RejectOrder` y `ReserveStock` pasan
por MediatR por dos razones: el `ValidationBehavior` corre FluentValidation antes de cualquier
handler, y los consumidores quedan en una línea que traduce el mensaje a un comando, de modo que
el handler no depende de `ConsumeContext` de MassTransit. Los `GET` leen el `DbContext` en su
propio slice: no tienen validación ni otro llamador, así que un handler no aportaría nada.

**Validación y errores.** El validador vive en su slice; el `ValidationBehavior` lanza la
excepción y `ApiExceptionHandler` la convierte en `ValidationProblemDetails` con las claves del
JSON (`clienteNombre`, `sku`, `cantidad`), que el frontend muestra bajo cada campo. El JSON mal
formado o con tipos incorrectos también responde `400` como `application/problem+json`. El
dominio (`Order.Create`) repite los límites con guard clauses: si fallan, es un error de
programación (`500`), no de entrada.

**EF Core sin repositorios.** El `DbContext` ya es unit of work, y el outbox necesita ese mismo
`DbContext`: `Publish` agrega `OrderCreated` al contexto y `SaveChanges` lo guarda junto con el
pedido. Un repositorio escondería ese acoplamiento sin quitarlo. Las dos sentencias SQL atómicas
de Inventory viven en el único slice que las usa.

**Configuración.** Options pattern con `ValidateDataAnnotations().ValidateOnStart()`: si falta la
cadena de conexión o una credencial de RabbitMQ, el servicio no arranca y el error nombra la clave.

El código está en inglés; el contrato JSON (`[JsonPropertyName]`) y los mensajes al usuario, en
español como el enunciado.

### Frontend

- React + Vite + TanStack Query, organizado por feature (`features/orders`, `features/inventory`).
  Todo el estado relevante es del servidor, así que no hay store global.
- El navegador solo habla con su propio origen: nginx (o Vite en desarrollo) hace de proxy de
  `/api` y `/hubs` hacia Orders API y de `/inventory-api` hacia Inventory Worker.
- zod valida con las mismas reglas que el backend. Si aun así llega un `400`, cada error se muestra
  bajo su campo; los errores sin campo (red, 5xx) van en un aviso en pantalla.
- La lista se actualiza por SignalR (`/hubs/orders`) y recarga completa al conectar o reconectar.
  Sin conexión, hace polling cada 5 s; con conexión, el polling se apaga.
- Agregué la página Inventario (`GET /products` del worker, polling cada 5 s) porque es la forma
  más rápida de ver el stock y comprobar el efecto de cada pedido sin entrar a la base de datos.
  Es solo lectura: el stock solo cambia al consumir `OrderCreated`.

### Kubernetes

`k8s/` tiene Orders API: `Deployment` (requests/limits, liveness en `/health/live`, readiness en
`/health/ready`), `Service` y `ConfigMap`. Las credenciales van en un `Secret` que no se versiona:

```bash
kubectl create secret generic orders-api-secrets \
  --from-literal=connection-string='Server=<host>,1433;Database=OrdersDb;User Id=<usuario>;Password=<password>;TrustServerCertificate=True' \
  --from-literal=rabbitmq-username='<usuario>' --from-literal=rabbitmq-password='<password>'
kubectl apply -f k8s/
```

Para el sistema completo: Inventory Worker como otro `Deployment` con su `Service`
(para `GET /products`), RabbitMQ y SQL Server gestionados o como `StatefulSet` con volumen
persistente, el panel como `Deployment` de nginx detrás de un `Ingress`, y las migraciones en un
`Job` previo al rollout en lugar de al arrancar.

## Trade-offs asumidos

- **Catálogo duplicado.** Orders valida el SKU contra su copia (`orders.KnownSkus`) para no
  depender de una llamada síncrona a Inventory; ambas copias se siembran por separado. Si
  divergen, el pedido se crea y Inventory lo rechaza con "el sku X no existe en inventario".
- **Push de SignalR sin garantía.** Se envía después del commit y fuera de cualquier outbox. Si el
  navegador está desconectado, el polling de 5 s y la recarga al reconectar lo ponen al día; si el
  envío falla con el navegador conectado, el cambio no aparece hasta la siguiente recarga de la
  lista.
- **Sin timeout para `Pending`.** Si el worker nunca vuelve, el pedido queda en `Pending`. Un
  timeout que lo rechace necesita compensación: si `StockReserved` llega después, el stock ya se
  descontó.
- **Una réplica de Orders API.** SignalR no tiene backplane: con varias réplicas, solo los
  navegadores conectados a la que consume el evento lo recibirían.
- **Una sola instancia de SQL Server** para las dos bases, para que el stack sea liviano.
- **Credenciales de desarrollo por defecto en `docker-compose.yml`** (y los servicios entran como
  `sa`), para que `docker compose up` funcione sin pasos previos. Se reemplazan con `.env`; fuera
  de desarrollo irían en un `Secret`, como en `k8s/`.

## Qué haría distinto con más tiempo

- Timeout para pedidos en `Pending` con un mensaje programado, y un evento de compensación que
  libere el stock si la reserva llega tarde.
- Catálogo sincronizado por eventos (`ProductRegistered` desde Inventory) en lugar de dos seeds.
- Backplane de Redis para SignalR y así poder escalar Orders API.
- Alertas sobre las colas `_error` y trazas (OpenTelemetry) entre `OrderCreated` y la confirmación.
- Paginación en `GET /orders` y un test end-to-end del panel.
