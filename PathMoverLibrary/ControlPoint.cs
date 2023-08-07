
namespace PathMover
{
    public class ControlPoint
    {
        public string Tag { get; set; }
        public ControlPoint()
        {
            Tag = "default";
        }
        public ControlPoint(string tag)
        {
            Tag = tag;
        }
        /*
        public bool operator ==(ControlPoint cp1, ControlPoint cp2)
        {
            if (cp1.Tag == cp2.Tag)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool operator !=(ControlPoint cp1, ControlPoint cp2)
        {
            if (cp1.Tag == cp2.Tag)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        */
    }

}
