/// <reference types="vite/client" />

interface Window {
    ENV?: {
        YARP_API_URL?: string;
        KEYCLOAK_URL?: string;
        KEYCLOAK_REALM?: string;
        KEYCLOAK_CLIENTID?: string;
    };
}