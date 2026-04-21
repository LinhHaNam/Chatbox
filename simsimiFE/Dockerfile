# Stage 1: Build React
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . ./
RUN npm run build

# Stage 2: Serve với Nginx
FROM nginx:alpine
# Lấy file từ thư mục dist đưa vào Nginx
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]