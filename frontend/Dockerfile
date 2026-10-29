FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .

# Solo para docker compose run frontend-tests; docker compose up construye el último stage (final).
FROM deps AS test
CMD ["npm", "test"]

FROM deps AS build
RUN npm run build

# Solo los estáticos compilados y nginx: ni Node ni node_modules llegan a la imagen final.
FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
