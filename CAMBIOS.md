# Guía de cambios sobre tu proyecto

Esta guía dice **qué archivo cambiar, qué poner y por qué**, en el orden en que conviene hacerlo.
El proyecto completo ya con todo aplicado viene en el mismo zip, para que compares.
Se respetaron tus nombres (`Business`, `Data`, `Models`, `Calcularvalor`→`CalcularValor`, etc.) y tu estilo (constructores primarios, `IRepositorio…`).

> No pude compilar (no hay SDK de .NET donde trabajo). Haz `dotnet build` y si sale algún error, mándamelo.

---

## 0. Mapa rápido

| # | Dónde | Qué | Criterio |
|---|-------|-----|----------|
| 1 | los 3 `.csproj` | Quitar `OutputType Exe` | 4 (compilar) |
| 2 | `Business/DTOs/**` | `class` con `private set` → `record` | 4 |
| 3 | proyecto nuevo `Api` | Controladores, Swagger, middleware | 4 |
| 4 | `Business/Interfaces/I…Servicio.cs` + servicios | Interfaces de servicio y DTOs de salida | 4 |
| 5 | `Models/Excepciones` | `NoEncontrado` y `Conflicto` | 4 |
| 6 | `Models/Entidades/Cliente*.cs` | Cliente abstracta + 3 subclases | 2 |
| 7 | `Models/Entidades/Servicio*.cs` | Propiedades abstractas nuevas, sin `PrecioBase` | 2 |
| 8 | `OrdendeTrabajo.cs`, `LineaServicio.cs`, `UsoRepuesto.cs` | Fecha prometida, valor, estados, líneas | 3 |
| 9 | `Repuesto.cs` | `Descontar` e `Ingresar` ahora sí cambian el stock | 3 |
| 10 | `Garantia.cs` | Generarla al entregar, casos de pérdida | 3 |
| 11 | `Data/**` | `Actualizar`, catálogo público, registro en DI | 4 |

---

## 1. Que compile y arranque

### 1.1 `Models.csproj`, `Business.csproj`, `Data.csproj`
**Por qué:** tienen `<OutputType>Exe</OutputType>` pero no tienen `Main`. Eso da el error CS5001. Son librerías.
**Cambio:** borra esa línea en los tres.

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

En `Business.csproj` y `Data.csproj` agrega además:

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
```

### 1.2 DTOs → `record`
**Dónde:** `Business/DTOs/Cliente/CrearClienteDto.cs`, `Motocicleta/CrearMotocicletaDto.cs`, `Orden/CrearOrdenDto.cs`.
**Por qué:** con `{ get; private set; }` o solo `{ get; }` y sin constructor, `System.Text.Json` no puede llenar el objeto: desde Swagger llegaría todo vacío y `dto.Lineas` daría `NullReferenceException`.
**Cambio:** conviértelos en `record` (también arreglé `Respuesto` → `Repuestos`, que habría quedado en el contrato JSON).

```csharp
public record CrearOrdenDto(
    int ClienteId, int MotocicletaId, int KilometrajeIngreso,
    List<LineaOrdenDto> Lineas, string? NumeroAutorizacionExterna = null);

public record LineaOrdenDto(string CodigoServicio, int MecanicoId, string Diagnostico = "",
    List<RepuestoOrdenDto>? Repuestos = null);

public record RepuestoOrdenDto(string Codigo, int Cantidad, bool ClienteAceptaEsperar = false);
```

Cambios de contenido en esos DTOs:
- `CrearOrdenDto` pierde `EsperaRepuesto`: ahora la espera se **calcula** (depende del origen de cada repuesto y del stock), no la dice el usuario.
- `CrearClienteDto` pierde `NumeroAutorizacionExterna` (es de la orden, no del cliente) y gana `DescuentoConvenio` (0.15–0.25).
- Se agregaron DTOs de **salida** (`ClienteDto`, `MotocicletaDto`, `OrdenDto`…) en los mismos archivos. Ver 1.4.

### 1.3 Proyecto nuevo `Api` (Web API)
**Dónde:** carpeta nueva `Api/` (reemplaza la consola "Hello, World", que puedes borrar).
**Por qué:** sin API no existe el criterio 4: no hay endpoints, controladores ni Swagger.
**Qué contiene:**
- `Api.csproj` (SDK Web + Swashbuckle, referencia a `Business` y `Data`).
- `Program.cs`: registra las capas y activa Swagger.
- `Middleware/ManejadorDeExcepciones.cs`: traduce excepciones a HTTP.
- `Controllers/` (Clientes, Motocicletas, Ordenes, Catalogo): **sin lógica**, solo reciben, llaman al servicio y devuelven.

```csharp
// Program.cs
builder.Services.AddData();       // registra los repositorios
builder.Services.AddBusiness();   // registra los servicios y el calendario
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen();
...
app.UseMiddleware<ManejadorDeExcepciones>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
```

Agrega `Api` y `Pruebas` al `.slnx`.

### 1.4 Interfaces de servicio y DTOs de salida
**Dónde:** `Business/Interfaces/IClienteServicio.cs`, `IMotocicletaServicio.cs`, `IOrdenServicio.cs`, `ICatalogoServicio.cs` (nuevos) y las clases en `Business/Servicios/`.
**Por qué:** la rúbrica pide que las capas se comuniquen **por interfaces** y que se usen **DTOs distintos de las entidades**. Tus servicios eran clases concretas que devolvían entidades (`Cliente`, `Motocicleta`…).
**Cambio:**
- Cada servicio implementa su interfaz (`public class ClienteServicio(...) : IClienteServicio`) y devuelve DTOs.
- Renombra `ClienteServivio.cs` → `ClienteServicio.cs`.
- Nuevo `Business/Mapeos/Mapeador.cs` convierte entidad → DTO.
- Nuevo `BusinessServiceCollectionExtensions.cs` con `AddBusiness()`.
- `ObtenerPorId` que devolvía `null` ahora lanza `NoEncontradoException` (la API responde 404).
- Se quitó `CambiarEstado(id, estado)` público: dejaba a cualquiera saltarse las reglas. Ahora hay métodos con nombre: `Autorizar`, `IniciarEjecucion`, `Terminar`, `Entregar`…

### 1.5 Excepciones
**Dónde:** `Models/Excepciones/` (agregar dos archivos).
**Por qué:** con una sola `ReglaNegocioException`, la API respondería todo con el mismo código. La rúbrica pide traducir las excepciones propias a respuestas HTTP claras.

```csharp
public class ReglaNegocioException(string message) : Exception(message) { }                 // 422
public class NoEncontradoException(string message) : ReglaNegocioException(message) { }    // 404
public class ConflictoException(string message) : ReglaNegocioException(message) { }       // 409
```

Y reemplaza los `InvalidOperationException` / `ArgumentException` sueltos por estas:
- "no existe…" → `NoEncontradoException`
- "ya existe…", transición de estado inválida, stock insuficiente → `ConflictoException`
- el resto → `ReglaNegocioException`

---

## 2. Criterio 2 — quitar los `switch` por tipo

### 2.1 `Cliente` pasa a ser abstracta
**Dónde:** `Models/Entidades/Cliente.cs` (modificar) + `ClienteParticular.cs`, `ClienteFlota.cs`, `ClienteConvenio.cs` (nuevos).
**Por qué:** tenías `switch (Tipo)` en `ObtenerDescuento`, en `CalcularFechaPrometida` y `if (cliente.Tipo == Convenio)` en `OrdenServicio.Crear`. La rúbrica pide que el polimorfismo **elimine** esos `if/switch`. Además `CupoCredito`, `MontoAutorizado` y `TieneFacturasVencidas` solo le sirven a flota, y `NumeroAutorizacionExterna` solo a convenio.

```csharp
public abstract class Cliente
{
    public abstract TipoCliente Tipo { get; }
    public abstract double FactorFila { get; }            // 1.0 flota, 1.5 convenio, 2.0 particular
    public abstract decimal ObtenerDescuento();            // tu mismo nombre de método
    public abstract bool AutorizaSinConsulta(decimal valor);
    public virtual void ValidarPuedeAbrirOrden(string? numeroAutorizacionExterna) { }
    public virtual void RegistrarCargo(decimal valor) { }
}
```

| Subclase | Descuento | Autoriza sin consulta | Bloquea abrir orden |
|----------|-----------|-----------------------|---------------------|
| `ClienteParticular` | **0** (tenías 5 %) | nunca | — |
| `ClienteFlota` | 0.12 | si `valor <= MontoAutorizado` | si superó el cupo **y** tiene facturas vencidas |
| `ClienteConvenio` | negociado, 0.15–0.25 (tenías 0.20 fijo) | siempre | si no hay número de autorización externo |

Y en `ClienteServicio.Crear`, donde antes hacías `new Cliente(...)`, ahora llamas a la fábrica `FabricaClientes.Crear(id, dto)` (archivo nuevo en `Business/Fabricas`): es el **único** lugar con `switch` por tipo, y es válido porque ahí se decide qué crear.

En `OrdenServicio.Crear` se borra el chequeo de convenio y queda una línea:

```csharp
cliente.ValidarPuedeAbrirOrden(dto.NumeroAutorizacionExterna);
```

### 2.2 `Servicio` y subclases
**Dónde:** `Servicio.cs`, `Preventivo.cs`, `Correctivo.cs`, `AjusteRapido.cs`.
**Por qué:**
- `PrecioBase` no se usaba en ningún cálculo (el valor sale de minutos × tarifa del mecánico). Se quita de los constructores y del catálogo.
- `DiasGarantia` y `KilometrosGarantia` existían pero nadie los leía; ahora se usan al entregar (ver 3.4).
- Faltaban dos propiedades abstractas que cambian por familia: `RequiereDiagnosticoPrevio` (solo Correctivo) y `EntraAFila` (Ajuste rápido no).
- `Correctivo.DiagnosticoFalla` nunca se asignaba. El diagnóstico es de cada caso, así que vive en `LineaServicio.Diagnostico`.
- `AjusteRapido` valida que dure menos de 30 minutos (para eso pasó de constructor primario a constructor normal).

---

## 3. Criterio 3 — reglas de negocio

### 3.1 `OrdendeTrabajo.cs` — fecha prometida
**Por qué:** tu fórmula no reproduce ninguno de los 5 ejemplos del documento de dominio:

| Caso | Tu fórmula | Documento |
|------|-----------|-----------|
| Ajuste rápido, particular | 2 días | mismo día |
| Preventivo, particular | 2 días | 1 día |
| Motor, particular | 3 días | 4 días |
| Motor, flota | 1 día | 2 días |
| Correctivo importado, flota | 2 días | 9 días |

Además `AddDays` cuenta días de calendario (puede caer en domingo o festivo) y la espera era un `+2` fijo.
**Cambio:** minutos totales ÷ minutos de la jornada × `cliente.FactorFila`, redondeado hacia arriba, **más** la espera del repuesto más lento (1/3/8 días según el origen), contando solo días hábiles:

```csharp
public DateTime CalcularFechaPrometida(Cliente cliente, ICalendarioLaboral calendario)
{
    int diasDeEjecucion = 0;
    if (_lineas.Any(l => l.Servicio.EntraAFila))
    {
        int minutos = _lineas.Sum(l => l.Servicio.MinutosEstimados);
        double jornadas = (double)minutos / calendario.MinutosProductivosPorDia * cliente.FactorFila;
        diasDeEjecucion = (int)Math.Ceiling(jornadas);
    }
    FechaEntregaPrometida = calendario.SumarDiasHabiles(FechaIngreso, diasDeEjecucion + DiasEsperaDeRepuestos());
    return FechaEntregaPrometida.Value;
}
```

`ICalendarioLaboral` es una interfaz nueva (`Models/Interfaces`) y `CalendarioColombia` (`Business/Calendario`) la implementa con los festivos. Así la entidad no depende de la capa de negocio.

### 3.2 `OrdendeTrabajo.cs` — valor, estados y líneas
- `Calcularvalor(...)` → `CalcularValor(Cliente cliente)`. Tu fórmula ya era correcta; deja de recibir dos `Func` porque ahora la línea guarda el `Mecanico` y los `UsoRepuesto` (ver 3.3).
- **Estados:** el enum ahora es el del documento (`Recibida, EnDiagnostico, Cotizada, Autorizada, EnEjecucion, EnEsperaDeRepuesto, Terminada, Entregada, CerradaSinEjecutar`; también desaparece el typo `Autoriazada`). Las transiciones válidas están en un diccionario:

```csharp
[EstadoOrden.EnEjecucion] = new[] { EstadoOrden.EnEsperaDeRepuesto, EstadoOrden.Terminada, EstadoOrden.Cotizada },
```

- **Retroceso:** `AgregarLinea` en una orden `Autorizada/EnEjecucion/EnEsperaDeRepuesto` la devuelve a `Cotizada` (falla adicional: se recotiza y se vuelve a autorizar).
- **Encapsulamiento:** `Lineas` era una `List` pública (cualquiera podía hacer `orden.Lineas.Add(...)` saltándose las reglas). Ahora es `private readonly List<…> _lineas` y se expone como `IReadOnlyList`.
- **`NumeroAutorizacionExterna`** se recibe en el constructor de la orden (antes era una propiedad que nadie asignaba).
- Métodos nuevos: `Cotizar`, `Autorizar`, `IniciarEjecucion`, `MarcarEnEsperaDeRepuesto`, `Reanudar`, `Terminar`, `RegistrarEntrega`, `CerrarSinEjecutar`, `CotizacionVencida` (5 días hábiles).

### 3.3 `LineaServicio.cs` y `UsoRepuesto.cs` (nuevo)
**Por qué:** la línea guardaba `MecanicoId` y repuestos como tuplas `(codigo, cantidad)`, y por eso la orden necesitaba `Func` para buscar precios y tarifas. Y para saber si falta stock o cuántos días esperar, la línea tiene que conocer el `Repuesto`.
**Cambio:**
- `int MecanicoId` → `Mecanico Mecanico`. El constructor valida `mecanico.PuedeEjecutar(servicio)`: la regla vive en el dominio y se borra el chequeo duplicado de `OrdenServicio`.
- Tuplas → `UsoRepuesto` (repuesto + cantidad + **precio de venta del momento**, para que un cambio de precio no altere una cotización ya hecha).
- `Repuestos` es `IReadOnlyList`, con `AgregarRepuesto(...)`.

### 3.4 `Garantia.cs` y generarla
**Por qué:** nadie creaba garantías: `DiasGarantia` de cada servicio nunca se usaba. Además la `Garantia` calculaba el límite con el kilometraje de **ingreso** (la garantía corre desde la **entrega**) y no cubría los casos del documento.
**Cambio:**
- `OrdenDeTrabajo.RegistrarEntrega` llama a `linea.GenerarGarantias(Id, fecha, km)` y cada línea crea su garantía **solo si su familia la tiene** (aquí el polimorfismo de `Servicio.TieneGarantia` por fin se usa).
- `Garantia.Validar(...)` ahora devuelve `ResultadoGarantia(Vigente, Motivo)` y cubre: anulada, intervenida por otro taller, kilometraje inconsistente (menor al registrado), vencida por tiempo, vencida por kilometraje.

### 3.5 `Repuesto.cs` — el stock nunca cambiaba
**Por qué:** `Descontar` e `Ingresar` solo validaban `cantidad <= 0` y no tocaban `CantidadStock`.

```csharp
public void Descontar(int cantidad)
{
    if (cantidad <= 0) throw new ReglaNegocioException("La cantidad debe ser positiva");
    if (cantidad > CantidadStock) throw new ConflictoException($"Stock insuficiente de {Codigo}...");
    CantidadStock -= cantidad;
    UnidadesVendidasUltimoTrimestre += cantidad;
}
public void Ingresar(int cantidad) { ...; CantidadStock += cantidad; }
```

Se agregó `Origen` (`BodegaLocal`/`Medellin`/`Importado`) con `DiasEspera` (1/3/8) y `HayExistencias(int)`.

### 3.6 `OrdenServicio.Crear` — cuándo se consume el stock
**Por qué:** descontabas los repuestos **al crear** la orden, antes de que el cliente autorizara. Un repuesto se consume cuando el trabajo **empieza**.
**Cambio:** `Crear` ya no consume nada: arma la orden, la cotiza y listo. El consumo ocurre en `IniciarEjecucion`, y si falta un repuesto la orden pasa a `EnEsperaDeRepuesto`; `Reanudar` lo reintenta cuando ya entró mercancía.

---

## 4. Criterio 1 — modelo

Casi todo ya se cubrió arriba. Resumen de lo que mejora este criterio:
- Colecciones internas privadas (`_lineas`, `_repuestos`, `_garantias`).
- Reglas dentro de las entidades (nivel del mecánico en `LineaServicio`, bloqueos en cada `Cliente…`, estados en la orden).
- Clases nuevas con responsabilidad clara: `ClienteParticular/Flota/Convenio`, `UsoRepuesto`.

`Mecanico`, `Motocicleta` e `Inventario` **no se tocaron** (siguen igual que los tenías).

---

## 5. Capa `Data`

- `Data.csproj`: sin `Exe`; agrega el paquete de DI.
- `CatalogoMemoria`: pasa de `internal` a `public` (la API no podía registrarlo) y se actualiza la semilla (sin `PrecioBase`, con `Origen`, y un correctivo de motor).
- Los repositorios implementan `Actualizar(...)` (vacío en memoria; con SQLite será el `UPDATE`).
- `IRepositorioCatalogo`: `ConsumirRepuesto` se reemplaza por `ActualizarRepuesto`.
- Nuevo `DataServiceCollectionExtensions.AddData()`.

> Nota de arquitectura: en tu diseño las interfaces de repositorio están en `Business` y `Data` las implementa (inversión de dependencias). Está bien y es defendible; en la sustentación di que `Business` no depende de `Data`, y que la API es quien conecta ambas.

---

## 6. Probar en Swagger

`dotnet run --project Api` abre `http://localhost:5080/swagger`.

1. `POST /api/clientes` → `{"documento":"1020304050","nombre":"Juan","telefono":"300","correo":"j@c.com","tipo":"Particular"}` (queda con `id` 1)
2. `POST /api/motocicletas` → `{"placa":"ABC12D","marca":"Yamaha","modelo":"FZ","cilindraje":150,"kilometraje":12000,"clienteId":1}`
3. `GET /api/catalogo/servicios` y `/mecanicos` para ver los códigos e ids.
4. `POST /api/ordenes` →
   `{"clienteId":1,"motocicletaId":1,"kilometrajeIngreso":12500,"lineas":[{"codigoServicio":"PREV-01","mecanicoId":1,"repuestos":[{"codigo":"ACE-01","cantidad":1}]}]}`
   → el preventivo no pide autorización: queda **Autorizada**, con `valorTotal` y `fechaEntregaPrometida`.
5. `iniciar-ejecucion` → `terminar` → `entregar` → `GET /api/ordenes/1/garantias?kilometrajeActual=12800`.

Para ver las reglas:
- Cliente `tipo:"Convenio"` con `descuentoConvenio:0.2`: crear una orden sin `numeroAutorizacionExterna` → **422**.
- Orden con `COR-01` sin `diagnostico` → **422**; con diagnóstico (mecánico 2, especialista) queda **Cotizada** y espera `/autorizar`.
- Repuesto `PAS-01` (importado, stock 0) con `clienteAceptaEsperar:false` → **422**; con `true`, la fecha prometida suma 8 días hábiles.
- Orden inexistente → **404**; segunda orden para la misma moto → **409**.

Las pruebas automáticas (`dotnet test Pruebas`) reproducen los ejemplos del documento de dominio.

---

## 7. Lo que no se cubrió (para después)

- Cobrar el diagnóstico cuando se cierra sin ejecutar y la regla del 10 % de re-autorización.
- Garantía propia del repuesto cuando el fabricante da más que el taller; orden de garantía sin costo.
- Rotación real por trimestre (hoy `UnidadesVendidasUltimoTrimestre` solo suma) y un endpoint para `Inventario`.
- Reportes del documento, bodegaje/abandono, SQLite, front web y documentación final.
