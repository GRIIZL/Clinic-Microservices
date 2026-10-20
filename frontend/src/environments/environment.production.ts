export const environment = {
  production: true,
  // В production фронт и Gateway должны жить за одним доменом (reverse-proxy),
  // либо здесь указывается абсолютный адрес шлюза.
  apiUrl: '/api',
};
