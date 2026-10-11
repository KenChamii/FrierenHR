// Production.
//
// The API is deployed as its own service on its own domain, so the full URL is baked in at
// build time. That makes every call cross-origin, which is why the API's Cors:AllowedOrigins
// has to list wherever this bundle is served from.
//
// Running the docker-compose stack instead puts nginx in front of both, serving the app and
// proxying /api to the API on one origin. For that, set these back to '' and '/hubs/chat'.
export const environment = {
  production: true,
  apiUrl: 'https://frierenhr.onrender.com',
  hubUrl: 'https://frierenhr.onrender.com/hubs/chat'
};
