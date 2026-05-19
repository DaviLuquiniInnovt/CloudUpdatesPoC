# Cloud Updates Insights — Frontend

Frontend Angular 21 (standalone components + Tailwind CSS) para a PoC de RAG que analisa
updates da AWS e Azure. Consome a API .NET local em `http://localhost:5267`.

## Stack

- Angular 21 com standalone components e signals
- Tailwind CSS 4 (via `@tailwindcss/postcss`)
- `marked` para renderização do Markdown retornado pelo LLM
- HttpClient nativo do Angular

## Pré-requisitos

- Node.js 20.11+ / 22+
- npm 10+
- Backend `CloudUpdatesPoC.Backend` rodando em `http://localhost:5267`

## Setup

```bash
npm install
npm start    # ou: ng serve
```

Abra `http://localhost:4200/`.

## ⚠️ CORS no backend (passo obrigatório, leva 30s)

O backend é ASP.NET Core e provavelmente **não tem CORS** configurado para
`http://localhost:4200`. Sem isso o browser bloqueia todas as chamadas.

No `Program.cs` do backend, adicione antes do `builder.Build()`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});
```

E depois do `var app = builder.Build();`, antes dos `Map*`:

```csharp
app.UseCors();
```

> Em produção troque `AllowAnyOrigin()` por `WithOrigins("http://localhost:4200")`.

## Endpoints consumidos

| Método | Endpoint  | Uso                                                                  |
| ------ | --------- | -------------------------------------------------------------------- |
| GET    | `/stats`  | Contadores no header (total indexado + por provider)                 |
| GET    | `/health` | Indicador de status (ping a cada 30s)                                |
| POST   | `/query`  | Pergunta principal. Body: `{ services: string[], question: string }` |

A URL base está em [`src/app/services/api.service.ts`](src/app/services/api.service.ts) na
constante `API_BASE_URL`. Mude lá se rodar o backend em outra porta.

## Estrutura

```
src/
├── app/
│   ├── app.ts                       # Componente raiz, compõe a tela
│   ├── app.config.ts                # Providers (HttpClient)
│   ├── models/api.types.ts          # Interfaces dos DTOs da API
│   ├── services/api.service.ts      # stats(), health(), query()
│   └── components/
│       ├── header/                  # Logo + badges + status da API
│       ├── query-form/              # Chip input + textarea + botão
│       ├── answer-card/             # Renderiza markdown da resposta
│       └── sources-panel/           # Cards de sources com score
├── index.html
├── main.ts
└── styles.css                       # Tailwind + tipografia do markdown
```

## Estados tratados

- **Carregamento de stats**: skeleton no header
- **Carregamento da query**: skeleton no card de resposta + spinner no botão
- **Erro 429**: alerta amarelo "Limite de uso atingido"
- **API offline (status 0)**: alerta cinza com instrução de subir o backend
- **Erro 5xx**: alerta vermelho com status
- **Resposta sem sources**: mensagem informando que nada relevante foi encontrado

## Scripts

| Comando         | O que faz                                |
| --------------- | ---------------------------------------- |
| `npm start`     | Dev server em `http://localhost:4200`    |
| `npm run build` | Build de produção em `dist/`             |
| `npm run watch` | Build em modo watch (dev)                |
| `npm test`      | Testes unitários com Vitest              |
