# Introduction

Give a brief introduction to MeldingerReceiver, and explain its purpose.

## 🧑‍💻 Usage

Add to your service collection:

```csharp
public static IServiceCollection AddServices

    (
        this IServiceCollection services,
        DatabaseConfiguration databaseConfiguration
    ) {
        services.AddScoped<IMeldingerConsumer, MyMeldingConsumer>();
        services.AddMeldingerReceiverWithBackgroundService(
            new ValkeyConfiguration { ... },
            new MeldingerReceiverApiConfiguration { ... },
            serviceProvider => serviceProvider.GetRequiredService<IMeldingerConsumer>()
        );
        return services;
    }
```
