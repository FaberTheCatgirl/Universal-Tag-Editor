﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagTool.Bitmaps.DDS;
using TagTool.Cache;
using TagTool.Common;
using TagTool.Direct3D.D3D9;
using TagTool.Direct3D.Xbox360;
using TagTool.Tags;
using TagTool.Tags.Definitions;
using TagTool.Tags.Resources;

namespace TagTool.Bitmaps
{
    public static class BitmapUtils
    {
        /// <summary>
        /// Get the virtual size of an Xbox 360 bitmap.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="minimalSize"></param>
        /// <returns></returns>
        public static int GetVirtualSize(int size, int minimalSize)
        {
            return (size % minimalSize == 0) ? size : size + (minimalSize - (size % minimalSize));
        }

        public static int GetXboxImageSize(XboxBitmap xboxBitmap)
        {
            // add special case for bitmaps not in virtual height/wdith

            int size;
            int dataWidth;
            int dataHeight;
            if (xboxBitmap.NotExact)
            {
                dataWidth = xboxBitmap.VirtualWidth;
                dataHeight = xboxBitmap.VirtualHeight;
            }
            else if (!xboxBitmap.MultipleOfBlockDimension)
            {
                dataWidth = xboxBitmap.NearestWidth;
                dataHeight = xboxBitmap.NearestHeight;
            }
            else
            {
                dataWidth = xboxBitmap.Width;
                dataHeight = xboxBitmap.Height;
            }

            size = (int)(dataWidth * dataHeight / xboxBitmap.CompressionFactor);

            switch (xboxBitmap.Type)
            {
                case BitmapType.CubeMap:
                    size *= 6;
                    break;
                case BitmapType.Texture3D:
                case BitmapType.Array:
                    size *= xboxBitmap.Depth;
                    break;
            }
            return size;
        }

        public static int GetXboxImageSize(Bitmap.Image image)
        {
            return GetXboxImageSize(new XboxBitmap(image));
        }

        public static int GetImageSize(BaseBitmap bitmap)
        {
            return (int)(bitmap.NearestHeight * bitmap.NearestWidth / bitmap.CompressionFactor);
        }

        public static int GetXboxMipMapSize(XboxBitmap xboxBitmap)
        {
            if (xboxBitmap.MipMapCount <= 1)
                return 0;
            else
            {
                var totalSize = 0;
                var mipMapCount = xboxBitmap.MipMapCount - 1;

                var curWidth = xboxBitmap.Width;
                var curHeight = xboxBitmap.Height;

                while (mipMapCount != 0)
                {
                    var nextMipWidth = curWidth / 2;
                    var nextMipHeight = curHeight / 2;
                    totalSize += GetSingleMipMapSize(nextMipWidth, nextMipHeight, xboxBitmap.MinimalBitmapSize, xboxBitmap.CompressionFactor);
                    mipMapCount--;
                }

                switch (xboxBitmap.Type)
                {
                    case BitmapType.CubeMap:
                        totalSize *= 6;
                        break;
                    case BitmapType.Texture3D:
                    case BitmapType.Array:
                        totalSize *= xboxBitmap.Depth;
                        break;
                }

                return totalSize;
            }  
        }

        public static int GetXboxMipMapSize(Bitmap.Image image)
        {
            return GetXboxMipMapSize(new XboxBitmap(image));
        }

        public static int GetMipMapSize(BaseBitmap blamBitmap)
        {
            int pixelCount = 0;
            var mipMapCount = blamBitmap.MipMapCount;

            if (blamBitmap.MipMapCount > 0)
            {
                int previousHeight = blamBitmap.Height;
                int previousWidth = blamBitmap.Width;

                for (int i = 0; i < mipMapCount; i++)
                {
                    var mipMapHeight = previousHeight / 2;
                    var mipMapWidth = previousWidth / 2;

                    previousHeight /= 2;
                    previousWidth /= 2;

                    pixelCount += mipMapHeight * mipMapWidth;
                }
            }
            return (int)(pixelCount / blamBitmap.CompressionFactor);

        }

        public static int NextNearestSize(int curSize, int minSize)
        {
            return minSize * ((curSize/2 + (minSize - 1)) / minSize);
        }
        
        public static int RoundSize(int size, int blockDimension)
        {
            return blockDimension * ((size + (blockDimension - 1)) / blockDimension);
        }

        public static int GetSingleMipMapSize(int width, int height, int minimalSize, double compressionFactor)
        {
            int virtualWidth = GetVirtualSize(width, minimalSize);
            int virtualHeight = GetVirtualSize(height, minimalSize);
            int size = (int)(virtualHeight * virtualWidth / compressionFactor); ;
            return size;
        }

        public static bool IsPowerOfTwo(int size)
        {
            return (size != 0) && ((size & (size - 1)) == 0);
        }

        public static BitmapTextureInteropDefinition CreateBitmapTextureInteropDefinition(BaseBitmap bitmap)
        {
            var result = new BitmapTextureInteropDefinition
            {
                Width = (short)bitmap.Width,
                Height = (short)bitmap.Height,
                Depth = (byte)bitmap.Depth,
                MipmapCount = (byte)(bitmap.MipMapCount + 1),
                HighResInSecondaryResource = 0,
            };

            result.BitmapType = bitmap.Type;
            result.Format = bitmap.Format;

            switch (result.Format)
            {
                case BitmapFormat.Dxt1:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT1;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxt3:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT3;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxt5:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT5;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxn:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_ATI2;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                default:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_UNKNOWN;
                    break;
            }

            result.Curve = bitmap.Curve;

            if (IsPowerOfTwo(bitmap.Width) && IsPowerOfTwo(bitmap.Height))
                result.Flags |= BitmapFlags.PowerOfTwoDimensions;

            return result;
        }

        public static BitmapTextureInteropDefinition CreateBitmapTextureInteropDefinition(DDSHeader header)
        {
            var result = new BitmapTextureInteropDefinition
            {
                Width = (short)header.Width,
                Height = (short)header.Height,
                Depth = (byte)header.Depth,
                MipmapCount = (byte)header.MipMapCount,
                HighResInSecondaryResource = 0,
            };

            result.BitmapType = BitmapDdsFormatDetection.DetectType(header);
            result.Format = BitmapDdsFormatDetection.DetectFormat(header);

            switch (result.Format)
            {
                case BitmapFormat.Dxt1:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT1;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxt3:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT3;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxt5:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT5;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxn:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_ATI2;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                default:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_UNKNOWN;
                    break;
            }

            result.Curve = BitmapImageCurve.xRGB; // find a way to properly determine that

            if (header.PixelFormat.Flags.HasFlag(DDSPixelFormatFlags.Compressed))
                result.Flags |= BitmapFlags.Compressed;

            if (IsPowerOfTwo(header.Width) && IsPowerOfTwo(header.Height))
                result.Flags |= BitmapFlags.PowerOfTwoDimensions;

            return result;
        }

        public static Bitmap.Image CreateBitmapImageFromResourceDefinition(BitmapTextureInteropDefinition definition)
        {
            var result = new Bitmap.Image()
            {
                Signature = "mtib",
                Width = definition.Width,
                Height = definition.Height,
                Depth = (sbyte)definition.Depth,
                Format = definition.Format,
                Type = definition.BitmapType,
                MipmapCount = (sbyte)(definition.MipmapCount != 0 ? definition.MipmapCount - 1 : 0),
                Flags = definition.Flags,
                Curve = definition.Curve
            };
            return result;
        }

        public static BitmapTextureInteropResource CreateEmptyBitmapTextureInteropResource()
        {
            return new BitmapTextureInteropResource
            {
                Texture = new D3DStructure<BitmapTextureInteropResource.BitmapDefinition>
                {
                    Definition = new BitmapTextureInteropResource.BitmapDefinition
                    {
                        PrimaryResourceData = new TagData(),
                        SecondaryResourceData = new TagData(),
                        Bitmap = new BitmapTextureInteropDefinition()
                    },
                    AddressType = CacheAddressType.Definition
                }
            };
        }

        public static BitmapTextureInteropResource CreateBitmapTextureInteropResource(BaseBitmap bitmap)
        {
            var result = CreateEmptyBitmapTextureInteropResource();
            result.Texture.Definition.PrimaryResourceData.Data = bitmap.Data;
            result.Texture.Definition.Bitmap = CreateBitmapTextureInteropDefinition(bitmap);
            return result;
        }

        public static void SetBitmapImageData(BaseBitmap bitmap, Bitmap.Image image)
        {
            image.Width = (short)bitmap.Width;
            image.Height = (short)bitmap.Height;
            image.Depth = (sbyte)bitmap.Depth;
            image.Format = bitmap.Format;
            image.Type = bitmap.Type;
            image.MipmapCount = (sbyte)bitmap.MipMapCount;
            image.DataSize = bitmap.Data.Length;
            image.XboxFlags = BitmapFlagsXbox.None;
            image.Flags = bitmap.Flags;
            image.Curve = bitmap.Curve;

            if (image.Format == BitmapFormat.Dxn)
                image.Flags |= BitmapFlags.Unknown3;

        }

        public static RealArgbColor DecodeBitmapPixelXbox(byte[] bitmapData, Bitmap.Image image, D3DTexture9 pTexture, int layerIndex, int mipmapIndex)
        {
            RealArgbColor result = new RealArgbColor(1,0,0,0);

            if( image.Type != BitmapType.Texture3D && (
                (image.Type == BitmapType.Texture2D && layerIndex == 1) || 
                (image.Type == BitmapType.CubeMap && layerIndex >= 0 && layerIndex < 6)  ||
                (image.Type == BitmapType.Array && layerIndex >= 0 && layerIndex < image.Depth)) && 
                (pTexture.GetBaseOffset() > 0 || pTexture.GetMipOffset() > 0))
            {
                uint blockWidth = 0;
                uint blockHeight = 0;
                int dataOffset = 0;
                int layerOffset = 0;

                // verify the mipmap level index 
                var minMipLevel = pTexture.GetMinMipLevel();
                var maxMipLevel = pTexture.GetMaxMipLevel();

                // if mipmapIndex is too low
                if (mipmapIndex < minMipLevel)
                    mipmapIndex = minMipLevel;
                // if mipmapIndex is too high
                if (mipmapIndex > maxMipLevel)
                    mipmapIndex = maxMipLevel;

                var currentHeight = image.Height >> mipmapIndex;
                var currentWidth = image.Width >> mipmapIndex;

                if (currentWidth < 1)
                    currentWidth = 1;

                if (currentHeight < 1)
                    currentHeight = 1;


                //XGGetTextureDesc
                XboxGraphics.XGTEXTURE_DESC textureDesc = new XboxGraphics.XGTEXTURE_DESC();

                
                XboxGraphics.XGGetBlockDimensions(XboxGraphics.XGGetGpuFormat(textureDesc.D3DFormat), out blockWidth, out blockHeight);

                layerOffset = 0; // Unknown XG function
                

                if (mipmapIndex > 0 && pTexture.GetMipOffset() > 0)
                    dataOffset = pTexture.GetMipOffset() + layerOffset;
                else
                    dataOffset = pTexture.GetBaseOffset() + layerOffset;

                for(int h = 0; h < currentHeight; h++)
                {
                    for(int w = 0; w < currentWidth; w++)
                    {

                    }
                }
            }


            return result;
        }

        public static uint GetXboxBitmapD3DTextureType(BitmapTextureInteropDefinition bitmapResource)
        {
            switch (bitmapResource.BitmapType)
            {
                case BitmapType.Texture2D:
                case BitmapType.Array:
                    return 1;
                case BitmapType.Texture3D:
                    return 2;
                case BitmapType.CubeMap:
                    return 3;
                default:
                    throw new NotSupportedException();
            }
        }

        public static uint GetXboxBitmapLevelOffset(BitmapTextureInteropDefinition bitmapResource, int ArrayIndex, int Level)
        {
            uint unknownType = GetXboxBitmapD3DTextureType(bitmapResource);

            var format = XboxGraphics.XGGetGpuFormat(bitmapResource.D3DFormat);
            uint bitsPerPixel = XboxGraphics.XGBitsPerPixelFromGpuFormat(format);

            int levels = bitmapResource.MipmapCount;
            if (levels == 0)
                levels = Direct3D.D3D9x.D3D.GetMaxMipLevels(bitmapResource.Width, bitmapResource.Height, bitmapResource.Depth, 0);

            bool isPacked = levels > 1;

            bool isTiled = Direct3D.D3D9x.D3D.IsTiled(bitmapResource.D3DFormat);
            int hasBorder = 0;

            uint width = (uint)bitmapResource.Width;
            uint height = (uint)bitmapResource.Height;
            uint depth = (uint)bitmapResource.Depth;

            uint borderSize = (uint)hasBorder * 2;
            uint logAdjustedWidth = Direct3D.D3D9x.D3D.Log2Ceiling((int)(width - borderSize));
            uint logAdjustedHeight = Direct3D.D3D9x.D3D.Log2Ceiling((int)(height - borderSize));
            uint logAdjustedDepth = 1;

            uint levelWidth = 1u << (int)(hasBorder + logAdjustedWidth);
            uint levelHeight = 1u << (int)(hasBorder + logAdjustedHeight);
            uint levelDepth = 1;

            uint rowPitch = 0;
            uint offset = 0;
            uint tailLevelOffset = 0;
            uint levelSizeBytes = 0;

            if (unknownType == 2)
            {
                logAdjustedDepth = Direct3D.D3D9x.D3D.Log2Ceiling((int)(depth - borderSize));
                levelDepth = 1u << (int)(hasBorder + logAdjustedDepth);
            }

            if (Level > 0 || (isPacked && (levelWidth <= 16 || levelHeight <= 16)))
            {
                uint widthAdjustment = 0;
                if ((width - borderSize) >> Level <= 1)
                    widthAdjustment = 1;
                else
                    widthAdjustment = (width - borderSize) >> Level;

                width = borderSize + widthAdjustment;

                uint heightAdjustment = 0;
                if ((height - borderSize) >> Level <= 1)
                    heightAdjustment = 1;
                else
                    heightAdjustment = (width - borderSize) >> Level;

                height = borderSize + heightAdjustment;

                if (unknownType == 2)
                {
                    uint depthAjustment = 0;
                    if ((depth - borderSize) >> Level <= 1)
                        depthAjustment = 1;
                    else
                        depthAjustment = (depth - borderSize) >> Level;

                    depth = depthAjustment;
                }
                else
                {
                    depth = 1;
                }

                uint arrayStride = 1;
                if (bitmapResource.BitmapType == BitmapType.CubeMap || bitmapResource.BitmapType == BitmapType.Array)
                {
                    int arrayFactor = bitmapResource.BitmapType == BitmapType.Array ? 2 : 0;
                    arrayStride = Direct3D.D3D9x.D3D.NextMultipleOf(bitmapResource.Depth, 1u << arrayFactor);
                }

                int logWidth = (int)Direct3D.D3D9x.D3D.Log2Floor((int)levelWidth);
                int logHeight = (int)Direct3D.D3D9x.D3D.Log2Floor((int)levelHeight);
                int logDepth = (int)Direct3D.D3D9x.D3D.Log2Floor((int)levelDepth);

                int levelIndex = Level;
                if (isPacked && (levelWidth <= 16 || levelHeight <= 16))
                {
                    ++logWidth;
                    ++logHeight;
                    ++logDepth;
                }
                else
                {
                    levelIndex = Level - 1;
                }

                int LevelIndex = Level;
                do
                {
                    if (logWidth > 0) --logWidth;
                    if (logHeight > 0) --logHeight;
                    if (logDepth > 0) --logDepth;

                    levelWidth = 1u << logWidth;
                    levelHeight = 1u << logHeight;
                    levelDepth = 1u << logDepth;

                    Direct3D.D3D9x.D3D.AlignTextureDimensions(ref levelWidth, ref levelHeight, ref levelDepth, bitsPerPixel, format, unknownType, isTiled);

                    rowPitch = (bitsPerPixel * levelWidth) / 8;

                    if (((1u << logWidth) <= 16 || (1u << logHeight) <= 16) && isPacked)
                    {
                        if (isPacked)
                        {
                            tailLevelOffset = Direct3D.D3D9x.D3D.GetMipTailLevelOffset(
                                levelIndex, 1 << logWidth, 1 << logHeight, 1 << logDepth, rowPitch, rowPitch * levelHeight, format);

                            offset += tailLevelOffset;
                            break;
                        }
                    }

                    levelSizeBytes = 0;
                    if (LevelIndex > 0)
                    {
                        if (unknownType == 2)
                            levelSizeBytes = Direct3D.D3D9x.D3D.NextMultipleOf(levelHeight * levelDepth * rowPitch, 4096);
                        else
                            levelSizeBytes = levelDepth * Direct3D.D3D9x.D3D.NextMultipleOf(levelHeight * rowPitch, 4096);
                    }

                    offset += arrayStride * levelSizeBytes;
                }
                while (--LevelIndex > 0);
            }
            else
            {
                levelWidth = width;
                levelHeight = height;
                levelDepth = depth;

                Direct3D.D3D9x.D3D.AlignTextureDimensions(
                    ref levelWidth, ref levelHeight, ref levelDepth, bitsPerPixel, format, unknownType, isTiled);

                if (!isTiled
                  && !(bitmapResource.BitmapType == BitmapType.Array)
                  && !(isPacked)
                  && unknownType == 1
                  && bitmapResource.MipmapCount < 1
                  && hasBorder == 0)
                {
                    uint blockWidth, blockHeight;
                    XboxGraphics.XGGetBlockDimensions(format, out blockWidth, out blockHeight);
                    levelHeight = Direct3D.D3D9x.D3D.NextMultipleOf(height, blockHeight);
                }

                if (unknownType > 0)
                    rowPitch = bitsPerPixel * 32 * levelWidth >> 3;
                else
                    rowPitch = bitsPerPixel * levelWidth >> 3;

                levelSizeBytes = Direct3D.D3D9x.D3D.NextMultipleOf(levelHeight * rowPitch, 4096);
                offset += levelSizeBytes * (uint)ArrayIndex;
            }

            return offset;
        }

        private static uint AlignToPage(uint offset)
        {
            return offset + 0xFFFu & ~0xFFFu;
        }
    }
}