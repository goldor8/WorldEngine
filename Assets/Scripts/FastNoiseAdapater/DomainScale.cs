namespace FastNoise
{
    public class DomainScale : FastNoise
    {
        private FastNoise _source;
        private float _scale;
        
        public DomainScale() : base("DomainScale")
        {
            _source = null;
            Scale = 1;
        }
        
        public DomainScale(FastNoise source) : base("DomainScale")
        {
            Source = source;
            Scale = 1;
        }
        
        public DomainScale(FastNoise source, float scale) : base("DomainScale")
        {
            Source = source;
            Scale = scale;
        }
        
        public FastNoise Source
        {
            get => _source;
            set
            {
                _source = value;
                Set("Source", value);
            }
        }
        
        public float Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                Set("Scale", value);
            }
        }
    }
}