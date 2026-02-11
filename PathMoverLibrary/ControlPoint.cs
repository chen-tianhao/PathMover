
namespace PathMover
{
    public class ControlPoint
    {
        public ushort Id { get; set; }
        public ControlPoint()
        {
            Id = 0;
        }
        public ControlPoint(ushort id)
        {
            Id = id;
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
