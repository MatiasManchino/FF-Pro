using FreightForwarder.Models;
using UnityEngine;

namespace FreightForwarder.Weather
{
    public class WeatherGrid
    {
        public int Width  { get; }
        public int Height { get; }

        private readonly WeatherCell[] _cells;

        public WeatherGrid(int width, int height)
        {
            Width  = width;
            Height = height;
            _cells = new WeatherCell[width * height];

            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new WeatherCell { noiseSeed = Random.value * 100f };
        }

        public WeatherCell GetCell(int x, int y)
        {
            x = (int)Mathf.Repeat(x, Width);
            y = Mathf.Clamp(y, 0, Height - 1);
            return _cells[y * Width + x];
        }

        // lat: -90..90, lon: -180..180
        public WeatherCell GetCellAtLatLon(float lat, float lon)
        {
            int x = Mathf.FloorToInt((lon + 180f) / 360f * Width)  % Width;
            int y = Mathf.FloorToInt((lat +  90f) / 180f * Height);
            y = Mathf.Clamp(y, 0, Height - 1);
            return _cells[y * Width + x];
        }

        public WeatherCell[] AllCells => _cells;
    }
}
