export const environment = {
  production: false,
  // В dev-режиме запросы идут через proxy.conf.json на Gateway (http://localhost:5000),
  // поэтому CORS не задействован.
  apiUrl: '/api',
};
