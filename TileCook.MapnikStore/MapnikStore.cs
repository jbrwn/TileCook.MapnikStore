using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETMapnik;
using TileCook;
using TileProj;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace TileCook.MapnikStore
{
    public class MapnikStore : ITileStore
    {
        private Map m { get; set;}

        public MapnikStore(string resource, string id = null)
        {
            Mapnik.RegisterDefaultInputPlugins();
            m = new Map(256, 256);
            m.BufferSize = 256;
            m.Load(resource);

            Id = id == null ? Path.GetFileNameWithoutExtension(resource) : id;
            Name = m.Parameters.ContainsKey("name") ? (string)m.Parameters["name"] : "";
            Description = m.Parameters.ContainsKey("description") ? (string)m.Parameters["description"] : "";
            MinZoom = m.Parameters.ContainsKey("minzoom") ? Convert.ToInt32(m.Parameters["minzoom"]) : 0;
            MaxZoom = m.Parameters.ContainsKey("maxzoom") ? Convert.ToInt32(m.Parameters["maxzoom"]) : 14;
            if (m.Parameters.ContainsKey("bounds"))
            {
                string[] bounds = ((string)m.Parameters["bounds"]).Split(',');
                Bounds = new Envelope(
                    Convert.ToDouble(bounds[0]),
                    Convert.ToDouble(bounds[1]),
                    Convert.ToDouble(bounds[2]),
                    Convert.ToDouble(bounds[3])
                );
            }
            else
            {
                Bounds = new Envelope(-180, -90, 180, 90);
            }

            if (m.Parameters.ContainsKey("json"))
            {
                JObject json = JObject.Parse((string)m.Parameters["json"]);
                JToken vector_layers;
                if (json.TryGetValue("vector_layers", out vector_layers))
                {
                    VectorLayers = vector_layers.ToObject<IEnumerable<VectorLayer>>();
                } 
            }

        }

        public VectorTile GetTile(ICoord coord)
        {
            SphericalMercator sm = new SphericalMercator();
            IEnvelope extent = sm.CoordToEnvelope(coord);
            m.ZoomToBox(extent.MinX, extent.MinY, extent.MaxX, extent.MaxY);

            //Convert Z to int 
            int z = (int)Math.Floor(coord.Z);

            NETMapnik.VectorTile tile = new NETMapnik.VectorTile(z, coord.X, coord.Y);
            m.Render(tile);
            string solidKey = tile.IsSolid();
            if (solidKey == null)
            {
                byte[] data = tile.GetData();
                using (MemoryStream memory = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                    {
                        gzip.Write(data, 0, data.Length);
                    }
                    return new VectorTile(memory.ToArray());
                }
            }
            // return null if tile is solid
            return null;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int MinZoom { get; private set; }
        public int MaxZoom { get; private set; }
        public IEnvelope Bounds { get; private set; }
        public IEnumerable<VectorLayer> VectorLayers { get; private set; }
    }
}
