#!/bin/sh

cat <<EOF > /usr/share/nginx/html/env.js
window.ENV = {
  YARP_API_URL: "${YARP_API_URL}",
  KEYCLOAK_URL: "${KEYCLOAK_URL}",
  KEYCLOAK_REALM: "${KEYCLOAK_REALM}",
  KEYCLOAK_CLIENTID: "${KEYCLOAK_CLIENTID}"
};
EOF

echo "[INFO] Injected env.js:"
cat /usr/share/nginx/html/env.js

exec nginx -g 'daemon off;'