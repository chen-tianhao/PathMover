using System.Collections.Generic;

namespace PathMoverRoutingGenerator
{
    // JSON structure classes for deserialization
    public class ControlPointData
    {
        public string id { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public string region { get; set; }
        public MetaData meta { get; set; }
        public bool inout { get; set; }
        public List<string> next { get; set; }
    }

    public class MetaData
    {
        public string kind { get; set; }
        public int? row { get; set; }
        public int? col { get; set; }
        public int? r1 { get; set; }
        public int? r2 { get; set; }
    }

    public class StepData
    {
        public double purple_horizontal { get; set; }
        public double grey { get; set; }
        public double orange { get; set; }
        public double green { get; set; }
        public int vertical_purple { get; set; }
        public int vertical_grey { get; set; }
    }

    public class Meta
    {
        public string excel { get; set; }
        public string sheet { get; set; }
        public StepData steps { get; set; }
        public List<string> notes { get; set; }
    }

    public class NetworkData
    {
        public Meta meta { get; set; }
        public List<ControlPointData> points { get; set; }
    }
}
