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
        services.AddMeldingerReceiver();
        return services;
    }
```

Inject into your class:

```csharp
public class MyService
{
    private readonly IMeldingerReceiver _samplePackage;

    public MyService(IMeldingerReceiver samplePackage)
    {
        _samplePackage = samplePackage;
    }

    public async Task DoSomething()
    {
        var result = await _samplePackage.Get();
        // Use result...
    }
}
```
