import Keycloak from 'keycloak-js';

const keycloak = new Keycloak({
  url: window.ENV?.KEYCLOAK_URL || 'http://localhost:8080',
  realm: window.ENV?.KEYCLOAK_REALM || 'eshop',
  clientId: window.ENV?.KEYCLOAK_CLIENTID || 'reactApp',
});

export default keycloak;