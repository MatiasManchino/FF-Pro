using System;
using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Systems.World
{

    // Genera noticias que reflejan el estado del mundo y disparan shocks en WorldStateManager.
    // Las noticias son consumidas por NewsTicker (UI existente) vía OnNewsPublished.

    public class NewsManager : Singleton<NewsManager>
    {
        public event Action<NewsItem> OnNewsPublished;

        private readonly List<NewsItem> _recent = new List<NewsItem>();
// Devuelve el recent noticias
        public IReadOnlyList<NewsItem> RecentNews => _recent;

        private static readonly NewsTemplate[] Templates = {
            new NewsTemplate("Crisis de combustible en el Golfo Pérsico",
                             "Los precios del crudo suben un 30% ante tensiones regionales.",
                             NewsCategory.Fuel, +0.35f, 0f, 0f),
            new NewsTemplate("Baja del precio del petróleo",
                             "La OPEP aumenta producción: combustible más barato para el transporte.",
                             NewsCategory.Fuel, -0.20f, 0f, 0f),
            new NewsTemplate("Boom del comercio electrónico",
                             "La demanda de logística global alcanza niveles récord.",
                             NewsCategory.Demand, 0f, +0.25f, 0f),
            new NewsTemplate("Recesión en mercados emergentes",
                             "La demanda de importaciones cae en Asia y Latinoamérica.",
                             NewsCategory.Demand, 0f, -0.20f, 0f),
            new NewsTemplate("Conflicto en el Mar Rojo",
                             "Las rutas marítimas hacia Europa se ven afectadas. Seguros suben.",
                             NewsCategory.Risk, 0f, 0f, +0.30f),
            new NewsTemplate("Temporada de huracanes en el Caribe",
                             "Rutas aéreas y marítimas desviadas por condiciones climáticas.",
                             NewsCategory.Risk, +0.10f, 0f, +0.20f),
            new NewsTemplate("Apertura de nuevas rutas comerciales",
                             "Acuerdos bilaterales reducen aranceles en rutas transpacíficas.",
                             NewsCategory.Demand, 0f, +0.15f, -0.05f),
            new NewsTemplate("Huelga portuaria global",
                             "Puertos en Europa y América paralizan operaciones por 72 horas.",
                             NewsCategory.Risk, +0.05f, -0.10f, +0.25f),
            new NewsTemplate("Innovación en logística verde",
                             "Nuevos buques de hidrógeno reducen costos operativos.",
                             NewsCategory.Fuel, -0.10f, 0f, 0f),
            new NewsTemplate("Festival de consumo global",
                             "Temporada navideña dispara la demanda de envíos express.",
                             NewsCategory.Demand, 0f, +0.20f, 0f),
        };

        // Titulares de ambientación (rescatados del juego anterior) que aparecen al azar en la cinta.
        private static readonly string[] BackgroundHeadlines = {
            "🚢 Congestión récord en puertos asiáticos retrasa los envíos varios días.",
            "📦 La falta de contenedores eleva las tarifas un 18%.",
            "⚓ Un puerto clave opera al 60% por inspecciones extraordinarias.",
            "✈ La saturación aérea encarece los envíos urgentes un 25%.",
            "Demoras en aduanas generan costos extra por carga.",
            "🚛 La escasez de choferes impacta la distribución terrestre.",
            "Embotellamiento logístico en rutas europeas genera demoras.",
            "Los fletes marítimos suben por la alta demanda.",
            "Fábricas reducen su producción por falta de energía.",
            "Demoras en transbordos afectan los cronogramas de entrega.",
            "Los fletes express superan precios históricos (+30%).",
            "Controles sanitarios ralentizan las importaciones de alimentos.",
            "El tiempo de tránsito global aumenta un 15%.",
            "El transporte terrestre sube tarifas por el combustible caro.",
            "Un perro callejero se vuelve famoso por bailar cumbia en la plaza.",
            "Aparece un mural de 30 metros pintado durante la noche por un artista anónimo.",
            "Maratón nocturna reúne a 2000 corredores; gana un cartero de 55 años.",
            "Ola de calor extremo de 42 grados paraliza la ciudad por tres días.",
            "Festival de música independiente llena el estadio tres noches seguidas.",
            "Se inaugura una ciclovía de 12 km que conecta el centro con la zona norte.",
        };

// Se ejecuta al iniciar el componente.
        private void Start()
        {
            if (FFTimeManager.Instance != null)
            {
                FFTimeManager.Instance.OnMonthPassed += OnMonthPassed;
                FFTimeManager.Instance.OnDayPassed   += OnDayPassed;
            }
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
            {
                FFTimeManager.Instance.OnMonthPassed -= OnMonthPassed;
                FFTimeManager.Instance.OnDayPassed   -= OnDayPassed;
            }
        }

// Se invoca al terminar un día de juego.
        private void OnDayPassed()
        {
            // ~12% de chance diaria de un titular de ambientación al azar.
            if (UnityEngine.Random.value > 0.12f) return;
            PublishHeadline(BackgroundHeadlines[UnityEngine.Random.Range(0, BackgroundHeadlines.Length)],
                            NewsCategory.General);
        }

        // Publica un titular simple en la cinta (sin shock de mundo).
        public void PublishHeadline(string headline, NewsCategory cat)
        {
            var item = new NewsItem(headline, "", cat, FFTimeManager.Instance?.GetFormattedDate() ?? "");
            if (_recent.Count >= 20) _recent.RemoveAt(0);
            _recent.Add(item);
            OnNewsPublished?.Invoke(item);
        }

// Se invoca cuando mes transcurre.
        private void OnMonthPassed()
        {
            // 60% de chance de noticia mensual
            if (UnityEngine.Random.value > 0.6f) return;

            var tmpl = Templates[UnityEngine.Random.Range(0, Templates.Length)];
            Publish(tmpl);
        }

// Gestiona publish.
        private void Publish(NewsTemplate tmpl)
        {
            var world = WorldStateManager.Instance;
            if (world == null) return;

            if (tmpl.FuelDelta   != 0) world.ApplyFuelShock(tmpl.FuelDelta, tmpl.Headline);
            if (tmpl.DemandDelta != 0) world.ApplyDemandShock(tmpl.DemandDelta, tmpl.Headline);
            if (tmpl.RiskDelta   != 0) world.ApplyRiskShock(tmpl.RiskDelta, tmpl.Headline);

            var item = new NewsItem(tmpl.Headline, tmpl.Body, tmpl.Category,
                                    FFTimeManager.Instance?.GetFormattedDate() ?? "");
            if (_recent.Count >= 20) _recent.RemoveAt(0);
            _recent.Add(item);

            OnNewsPublished?.Invoke(item);
            Debug.Log($"[News] {tmpl.Headline}");
        }

        public void PublishManual(string headline, string body, NewsCategory cat,
                                   float fuelDelta = 0, float demandDelta = 0, float riskDelta = 0)
        {
            var tmpl = new NewsTemplate(headline, body, cat, fuelDelta, demandDelta, riskDelta);
            Publish(tmpl);
        }
    }

    // Noticias category.

    public enum NewsCategory { Fuel, Demand, Risk, General }

    public class NewsItem
    {
// Gestiona headline.
        public string      Headline  { get; }
// Gestiona body.
        public string      Body      { get; }
// Gestiona category.
        public NewsCategory Category { get; }
// Gestiona date.
        public string      Date      { get; }

// Realiza noticias elemento
        public NewsItem(string headline, string body, NewsCategory cat, string date)
        {
            Headline = headline;
            Body     = body;
            Category = cat;
            Date     = date;
        }
    }

    internal class NewsTemplate
    {
// Gestiona headline.
        public string      Headline    { get; }
// Gestiona body.
        public string      Body        { get; }
// Gestiona category.
        public NewsCategory Category   { get; }
// Combustible delta.
        public float       FuelDelta   { get; }
// Demanda delta.
        public float       DemandDelta { get; }
// Riesgo delta.
        public float       RiskDelta   { get; }

// Realiza noticias template
        public NewsTemplate(string h, string b, NewsCategory c, float f, float d, float r)
        { Headline = h; Body = b; Category = c; FuelDelta = f; DemandDelta = d; RiskDelta = r; }
    }
}