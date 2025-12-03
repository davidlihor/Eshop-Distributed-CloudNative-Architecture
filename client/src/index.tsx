import React from 'react'
import ReactDOM from 'react-dom/client'
import './app/layout/styles.css'
import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import { RouterProvider } from 'react-router-dom';
import { router } from './app/router/Routes';
import { Provider } from 'react-redux';
import { store } from './app/store/configureStore';
import "slick-carousel/slick/slick.css";
import "slick-carousel/slick/slick-theme.css";
import { ReactKeycloakProvider } from '@react-keycloak/web';
import keycloak from './app/api/keycloak';
import { initUserFromKeycloak } from './features/account/accountSlice';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
      <Provider store={store}>
        <ReactKeycloakProvider authClient={keycloak}>
          <RouterProvider router={router} />
        </ReactKeycloakProvider>
      </Provider>
  </React.StrictMode>,
)

keycloak.init({
  onLoad: 'check-sso',
  pkceMethod: 'S256',
  checkLoginIframe: false,
  silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html'
}).then(authenticated => {
  if (authenticated) {
    store.dispatch(initUserFromKeycloak() as any);
  } else {
    store.dispatch({ type: 'account/initUserFromKeycloak/rejected' });
  }
}).catch(err => {
  console.error(err);
  store.dispatch({ type: 'account/initUserFromKeycloak/rejected' });
});