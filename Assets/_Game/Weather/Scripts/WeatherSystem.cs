using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Weather
{
    /// <summary>
    /// Orchestrates the weather simulation tick and notifies WeatherManager.
    /// Add this component to the FF System GameObject.
    /// </summary>
    public class WeatherSystem : Singleton<WeatherSystem>
    {
        [SerializeField] private WeatherConfig config;

        private WeatherGrid _grid;
        private float       _timer;
        private float       _timeOffset;

        public WeatherGrid Grid => _grid;

        protected override void OnAwake()
        {
            if (config == null)
                config = ScriptableObject.CreateInstance<WeatherConfig>();

            _grid = new WeatherGrid(config.gridWidth, config.gridHeight);
            PreWarmGrid();
        }

        // Popula el grid con valores realistas desde el primer frame.
        // Sin esto, la nube tarda ~50 segundos en llegar al threshold.
        private void PreWarmGrid()
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);
                    float nx = (x + cell.noiseSeed) * config.noiseScale;
                    float ny = (y + cell.noiseSeed) * config.noiseScale;

                    // FBM de 3 octavas para que el resultado sea creíble desde el inicio
                    float v = 0f, amp = 0.5f, freq = 1f, maxV = 0f;
                    for (int i = 0; i < 3; i++)
                    {
                        v    += Mathf.PerlinNoise(nx * freq, ny * freq) * amp;
                        maxV += amp;
                        amp  *= 0.5f; freq *= 2f;
                    }
                    cell.cloud = v / maxV;
                }
            }
        }

        private void Start() => Activate();

        // Puede llamarse explícitamente si el Start() del ciclo de Unity llega tarde
        public void Activate()
        {
            if (_activated) return;
            _activated = true;
            CloudRenderer.Instance?.Initialize(_grid, config);
            WeatherImpact.Instance?.Initialize(_grid, config);
        }

        private bool _activated;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < config.updateIntervalSeconds) return;

            _timeOffset += config.noiseTimeSpeed * _timer;
            if (_timeOffset > 1e6f) _timeOffset -= 1e6f;
            _timer = 0f;

            Tick();
        }

        private void Tick()
        {
            var cells = _grid.AllCells;
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = cells[y * _grid.Width + x];
                    WeatherSimulator.UpdateClouds(cell, x, y, _timeOffset, config);
                    WeatherSimulator.UpdateStorms(cell, config);
                    WeatherSimulator.UpdateCyclones(cell, config);
                }
            }

            CloudRenderer.Instance?.Refresh(_grid);
        }
    }
}
