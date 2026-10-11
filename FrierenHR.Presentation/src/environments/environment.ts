// Production. Empty apiUrl makes every call same-origin and relative, so the deployed
// frontend talks to whatever host serves it and the API hostname is never baked into the
// bundle. The reverse proxy in front of the container routes /api and /hubs to the API.
export const environment = {
  production: true,
  apiUrl: '',
  hubUrl: '/hubs/chat'
};
