﻿namespace BDArmory.Core.Utils
{
    internal class LayerMask
    {
        public static int CreateLayerMask(bool aExclude, params int[] aLayers)
        {
            int v = 0;
            foreach (var L in aLayers)
                v |= 1 << L;
            if (aExclude)
                v = ~v;
            return v;
        }

        public static int ToLayer(int bitmask)
        {
            int result = bitmask > 0 ? 0 : 31;
            while (bitmask > 1)
            {
                bitmask = bitmask >> 1;
                result++;
            }
            return result;
        }
    }
}
