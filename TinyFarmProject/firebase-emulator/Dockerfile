# 🔥 Java 21 có sẵn (Firebase Emulator yêu cầu)
FROM eclipse-temurin:21-jre

# Cài curl + nodejs 20
RUN apt-get update && \
    apt-get install -y curl ca-certificates && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean

# Cài Firebase CLI
RUN npm install -g firebase-tools

WORKDIR /firebase
COPY . .

# 🚀 Set environment variables để Unity client kết nối tới emulator
ENV USE_AUTH_EMULATOR=127.0.0.1:9099
ENV FIREBASE_DATABASE_EMULATOR_HOST=127.0.0.1:9000

EXPOSE 4000 9099 9000

CMD ["firebase", "emulators:start", "--only", "auth,database", "--project", "tinyfarmgameproject"]
