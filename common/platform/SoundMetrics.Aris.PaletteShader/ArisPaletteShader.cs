// Copyright 2010-2018 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SoundMetrics.Aris.PaletteShader
{
    /// <summary>
    /// Applies a specified palette as available in ARIScope and other places.
    /// </summary>
    public class ArisPaletteShader : ShaderEffect
    {
        /// <summary>
        /// Constructs the shader and initializes its resources.
        /// </summary>
        public ArisPaletteShader()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = new Uri(@"pack://application:,,,/SoundMetrics.Aris.PaletteShader;component/RenderShader.ps");
            shader.Freeze();
            PixelShader = shader;

            // Texture samplers 
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(PalletteProperty);

            // Shader constants 
            UpdateShaderValue(LoThresholdProperty);
            UpdateShaderValue(HiThresholdProperty);
            UpdateShaderValue(PalletteIndexProperty);
            UpdateShaderValue(InvertPalletteProperty);
            UpdateShaderValue(FlipImageProperty);
            UpdateShaderValue(ShaderIndexProperty);
            UpdateShaderValue(ImageSizeProperty);
        }

        /// <summary>
        /// The input texture: : Sampler #0
        /// </summary>
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ArisPaletteShader), 0);

        /// <summary>
        /// The pallette lookup texture: Sampler #1 
        /// </summary>
        public Brush Palette
        {
            get { return (Brush)GetValue(PalletteProperty); }
            set { SetValue(PalletteProperty, value); }
        }

        public static readonly DependencyProperty PalletteProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Pallette", typeof(ArisPaletteShader), 1);

        /// <summary>
        /// The low (cut-off) threshold 
        /// </summary>
        public float LoThreshold
        {
            get { return (float)GetValue(LoThresholdProperty); }
            set { SetValue(LoThresholdProperty, value); }
        }

        public static readonly DependencyProperty LoThresholdProperty =
            DependencyProperty.Register(
                "LoThreshold", typeof(float), typeof(ArisPaletteShader), new UIPropertyMetadata(0.0f, PixelShaderConstantCallback(0)));

        /// <summary>
        /// The high (saturation) threshold 
        /// </summary>
        public float HiThreshold
        {
            get { return (float)GetValue(HiThresholdProperty); }
            set { SetValue(HiThresholdProperty, value); }
        }

        public static readonly DependencyProperty HiThresholdProperty =
            DependencyProperty.Register(
                "HiThreshold", typeof(float), typeof(ArisPaletteShader), new UIPropertyMetadata(0.9f, PixelShaderConstantCallback(1)));

        /// <summary>
        /// The pallette index 
        /// </summary>
        public float PalletteIndex
        {
            get { return (float)GetValue(PalletteIndexProperty); }
            set { SetValue(PalletteIndexProperty, value); }
        }

        public static readonly DependencyProperty PalletteIndexProperty =
            DependencyProperty.Register(
            "PalletteIndex", typeof(float), typeof(ArisPaletteShader), new UIPropertyMetadata(0.0f, PixelShaderConstantCallback(2)));

        /// <summary>
        /// The Invert pallette flag
        /// </summary>
        public float InvertPallette
        {
            get { return (float)GetValue(InvertPalletteProperty); }
            set { SetValue(InvertPalletteProperty, value); }
        }

        public static readonly DependencyProperty InvertPalletteProperty =
            DependencyProperty.Register(
            "InvertPallette", typeof(float), typeof(ArisPaletteShader), new UIPropertyMetadata(0.0f, PixelShaderConstantCallback(3)));

        /// <summary>
        /// The Flip Image flag. 
        /// Defaults to true ( == 1.0f ) 
        /// </summary>
        public float FlipImage
        {
            get { return (float)GetValue(FlipImageProperty); }
            set { SetValue(FlipImageProperty, value); }
        }

        public static readonly DependencyProperty FlipImageProperty =
            DependencyProperty.Register(
            "FlipImage", typeof(float), typeof(ArisPaletteShader), new UIPropertyMetadata(0.0f, PixelShaderConstantCallback(4)));

        /// <summary>
        /// The Shader index. 
        /// Defaults to standard ( == 0.0f ) 
        /// </summary>
        public float ShaderIndex
        {
            get { return (float)GetValue(ShaderIndexProperty); }
            set { SetValue(ShaderIndexProperty, value); }
        }

        public static readonly DependencyProperty ShaderIndexProperty =
            DependencyProperty.Register(
            "ShaderIndex", typeof(float), typeof(ArisPaletteShader), new UIPropertyMetadata(0.0f, PixelShaderConstantCallback(5)));

        /// <summary>
        /// The Image Size (reciprocal). at #6
        /// Defaults to 96 / 512  
        /// </summary>
        public Vector ImageSize
        {
            get { return (Vector)GetValue(ImageSizeProperty); }
            set { SetValue(ImageSizeProperty, value); }
        }

        public static readonly DependencyProperty ImageSizeProperty =
            DependencyProperty.Register(
                "ImageSize",
                typeof(Vector),
                typeof(ArisPaletteShader),
                new UIPropertyMetadata(new Vector(1 / 96.0, 1 / 512.0), PixelShaderConstantCallback(6)));
    }
}
