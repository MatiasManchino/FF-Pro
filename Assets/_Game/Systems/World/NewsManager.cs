using System;
using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Systems.World
{
    /// <summary>
    /// Genera noticias que reflejan el estado del mundo y disparan shocks en WorldStateManager.
    /// Las noticias son consumidas por NewsTicker (UI existente) vía OnNewsPublished.
    /// </summary>
    public class NewsManager : Singleton<NewsManager>
    {
        public event Action<NewsItem> OnNewsPublished;

        private readonly List<NewsItem> _recent = new List<NewsItem>();
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

        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnMonthPassed += OnMonthPassed;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnMonthPassed -= OnMonthPassed;
        }

        private void OnMonthPassed()
        {
            // 60% de chance de noticia mensual
            if (UnityEngine.Random.value > 0.6f) return;

            var tmpl = Templates[UnityEngine.Random.Range(0, Templates.Length)];
            Publish(tmpl);
        }

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

    // ── Data types ────────────────────────────────────────────────────────────

    public enum NewsCategory { Fuel, Demand, Risk, General }

    public class NewsItem
    {
        public string      Headline  { get; }
        public string      Body      { get; }
        public NewsCategory Category { get; }
        public string      Date      { get; }

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
        public string      Headline    { get; }
        public string      Body        { get; }
        public NewsCategory Category   { get; }
        public float       FuelDelta   { get; }
        public float       DemandDelta { get; }
        public float       RiskDelta   { get; }

        public NewsTemplate(string h, string b, NewsCategory c, float f, float d, float r)
        { Headline = h; Body = b; Category = c; FuelDelta = f; DemandDelta = d; RiskDelta = r; }
    }
}
