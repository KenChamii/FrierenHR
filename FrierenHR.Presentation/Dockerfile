# Build context is this project directory.
#   docker build -f FrierenHR.Presentation/Dockerfile FrierenHR.Presentation

FROM node:20-alpine AS build
WORKDIR /src

# Lockfile first so npm ci is cached independently of source changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf

# The application builder emits to dist/<project>/browser.
COPY --from=build /src/dist/frieren-hr.presentation/browser /usr/share/nginx/html

EXPOSE 80
