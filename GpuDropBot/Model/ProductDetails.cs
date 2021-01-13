using System;

namespace GpuDropBot.Model
{
    public sealed class ProductDetails
    {
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public Uri Link { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ProductDetails && Link.AbsoluteUri == (obj as ProductDetails).Link.AbsoluteUri;
        }

        public override int GetHashCode()
        {
            return Link.AbsoluteUri.GetHashCode();
        }
    }
}
