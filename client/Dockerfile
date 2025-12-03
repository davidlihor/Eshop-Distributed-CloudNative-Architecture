FROM node:18-alpine AS builder
WORKDIR /app

COPY client/package*.json .
RUN npm install

COPY client .
RUN npm run build

FROM nginx:alpine

RUN addgroup --system --gid 1001 nginxgroup
RUN adduser --system --uid 1001 nginxuser

COPY --from=builder --chown=nginxuser:nginxgroup /app/dist /usr/share/nginx/html
RUN chown -R nginxuser:nginxgroup /usr/share/nginx/html

COPY client/nginx.conf /etc/nginx/conf.d/default.conf
COPY client/start.sh /start.sh

RUN chmod +x /start.sh
RUN mkdir -p /var/run /var/cache/nginx && chown nginxuser:nginxgroup /var/run /var/cache/nginx

USER nginxuser
EXPOSE 80

CMD ["/start.sh"]