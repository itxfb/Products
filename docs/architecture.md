# Architecture

How the Products service fits into an event-driven microservices system. Solid black boxes are built in this repository; dashed boxes are the target state.

```mermaid
flowchart TB
    spa["React SPA"]:::built
    idp["Identity provider<br/>Keycloak · issues JWTs"]:::built
    gateway["API gateway<br/>TLS · routing · rate limiting"]:::target

    subgraph services["Services: each validates JWTs and exports OpenTelemetry"]
        products["Products API"]:::built
        orders["Orders"]:::target
        payments["Payments"]:::target
        notifications["Notifications"]:::target
        productsDb[("Products DB<br/>PostgreSQL<br/>outbox: target")]:::built
        ordersDb[("Orders DB<br/>+ outbox")]:::target
        paymentsDb[("Payments DB<br/>+ outbox")]:::target
        notificationsDb[("Notifications DB<br/>+ outbox")]:::target
        products --> productsDb
        orders --> ordersDb
        payments --> paymentsDb
        notifications --> notificationsDb
    end

    broker{{"Message broker<br/>RabbitMQ · Azure Service Bus · Kafka<br/>ProductCreated · OrderPlaced<br/>PaymentSucceeded / PaymentFailed<br/>OrderConfirmed"}}:::target
    otel["OpenTelemetry collector<br/>→ observability backend"]:::target

    spa -- "OIDC code + PKCE" --> idp
    spa -- "HTTPS + JWT" --> gateway
    gateway --> services
    idp -. "JWKS" .-> services
    productsDb -.-> broker
    ordersDb <-.-> broker
    paymentsDb <-.-> broker
    notificationsDb <-.-> broker
    services -. "traces · metrics · logs" .-> otel

    subgraph legend["Legend"]
        builtKey["Built in this repo"]:::built
        targetKey["Target state"]:::target
    end

    classDef built fill:#111111,color:#ffffff,stroke:#111111,stroke-width:2px
    classDef target fill:#ffffff,color:#111111,stroke:#111111,stroke-width:2px,stroke-dasharray:6 4
```

![Products service within the target event-driven architecture](architecture.png)

- **Database per service.** Each service owns its schema; no service reads another service's tables.
- **Transactional outbox and idempotent consumers.** A service commits its state change and the outgoing event in one transaction, a relay publishes the outbox, and consumers record processed message ids so at-least-once delivery is safe.
- **Choreographed order → payment saga.** Orders publishes OrderPlaced; Payments charges and publishes PaymentSucceeded or PaymentFailed; Orders confirms or cancels and publishes OrderConfirmed; Notifications emails the customer.
- **Local read models instead of synchronous calls.** Orders keeps the product name and price it needs from ProductCreated, so it never calls Products at request time and an outage cannot cascade.
- **Stateless horizontal scaling.** Services validate JWTs locally against the identity provider's JWKS and hold no session state, so replicas can be added freely; EF Core's migration lock keeps simultaneous startups safe.
- **Cross-cutting concerns at the gateway.** TLS termination, routing and rate limiting live in the gateway; nginx stands in for it locally.
- **Observability.** Every service exports traces, metrics and logs through OpenTelemetry to one backend.
