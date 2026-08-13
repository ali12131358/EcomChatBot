# EcomChatBot (MVP)

فایل‌ها: Program.cs, ProductIndexer.cs, IndexController.cs, ChatController.cs, appsettings.json (placeholder)، docker-compose.yml

پیش‌نیازها
- .NET 7/8 SDK
- Docker (برای Qdrant)
- کلید OpenAI (نگهداری امن: user-secrets / env / GitHub Secrets)

روش امن قرار دادن کلید (توسعه محلی)
```bash
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
```

یا متغیر محیطی (Bash):
```bash
export OpenAI__ApiKey="sk-..."
```

راه‌اندازی Qdrant
```bash
docker compose up -d
# یا:
docker run -d --name qdrant -p 6333:6333 qdrant/qdrant
```

اجرای پروژه
```bash
dotnet run
```

ایندکس و تست
```bash
curl -X POST http://localhost:5000/api/index/ensure
curl -X POST http://localhost:5000/api/index/seed
curl -X POST http://localhost:5000/api/chat -H "Content-Type: application/json" -d "{\"message\":\"این گوشی رم 6 داره؟\"}"
```

نکات امنیتی
- کلید را در مخزن عمومی قرار ندهید. برای CI/Production از GitHub Secrets یا Key Vault استفاده کنید.
- قبل از اجرای workflow در GitHub، یک secret با نام OPENAI_API_KEY اضافه کنید.
