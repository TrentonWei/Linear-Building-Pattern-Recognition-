using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrDispalce.地图要素
{
    public abstract class MapObjectLayer
    {
        public abstract FeatureType FeatureType
        {
            get;
        }
    }

    public class PointLayer :MapObjectLayer
    {
        public override FeatureType FeatureType
        {
            get { return FeatureType.PointType; }
        }
        private List<PointObject> pointList;
        public List<PointObject> PointList
        {
            get { return this.pointList; }
            set { this.pointList = value; }
        }
    }

    public class PolylineLayer:MapObjectLayer
    {
        public override FeatureType FeatureType
        {
            get { return FeatureType.PolylineType; }
        }
        private List<PolylineObject> polylineList;
        public List<PolylineObject> PolylineList
        {
            get { return this.polylineList; }
            set { this.polylineList = value; }
        }
    }

    public class PolygonLayer:MapObjectLayer
    {
        public override FeatureType FeatureType
        {
            get { return FeatureType.PolygonType; }
        }
        private List<PolygonObject> polygonList;
        public List<PolygonObject> PolygonList
        {
            get { return this.polygonList; }
            set { this.polygonList = value; }
        }
    }
}
