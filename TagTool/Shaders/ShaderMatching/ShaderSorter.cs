using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagTool.Shaders.ShaderMatching
{
    //
    // Idea: create a distance metric between shaders. The input is the shader options and the output is a 1 dimensional coordinate. 
    // The metric is then used to find rmt2 that has the minimal superior value to the target shader. If a perfect match exists this would return the exact rmt2.
    //

    public static class Sorter
    {
        public static List<int> GetTemplateOptions(string name)
        {
            List<int> options = new List<int>();
            var optionStrings = name.Split('\\').ToList().Last().Split('_').ToList();
            optionStrings.RemoveAt(0);
            foreach (var optStr in optionStrings)
            {
                options.Add(int.Parse(optStr));
            }
            return options;
        }
        
        public static long GetValue(SortingInterface shaderInterface, List<int> current)
        {
            // assumes target and shaderInterface are from the same shader type
            int baseStepSize = 17; // max number of option + 1
            long value = 0;

            for(int i = 0; i < shaderInterface.GetTypeCount(); i++)
            {
                long typeScale = (long)Math.Pow(baseStepSize, shaderInterface.GetTypeIndex(i));
                value += typeScale * (shaderInterface.GetOptionIndex(i, current[i]) + 1);
            }
            return value;
        }
    }

    public interface SortingInterface
    {
        int GetTypeCount();
        int GetTypeIndex(int typeIndex);
        int GetOptionCount(int typeIndex);
        int GetOptionIndex(int typeIndex, int optionIndex);
        void PrintOptions(List<int> options);
        string ToString(List<int> options);
    }

    public class ShaderSorter : SortingInterface
    {
        //
        // TODO: order the list for best matches, the higher the index the higher the importance. (low -> high) (0 -> n). Options that can be easily adapted should have less importance
        // than options that cannot be replaced, same for types (for example, material model is critial, therefore it should have a higher sorted position than other types because when
        // it comes time to select a shader, the closest shader is more likely to have the same material model options.
        //

        // these private lists define the order of the types and options in a shader. The matcher will use these when no perfect match exists. 
        private static List<ShaderOptionTypes> TypeOrder = new List<ShaderOptionTypes> 
        {
            ShaderOptionTypes.misc,
            ShaderOptionTypes.environment_mapping,
            ShaderOptionTypes.parallax,
            ShaderOptionTypes.bump_mapping,
            ShaderOptionTypes.blend_mode,
            ShaderOptionTypes.alpha_test,
            ShaderOptionTypes.self_illumination,
            ShaderOptionTypes.specular_mask,
            ShaderOptionTypes.albedo,
            ShaderOptionTypes.material_model,
            ShaderOptionTypes.warp,
            ShaderOptionTypes.overlay,
            ShaderOptionTypes.blend_mode,
            ShaderOptionTypes.alpha_test,
            ShaderOptionTypes.alpha_blend_source,
            ShaderOptionTypes.wetness,
            ShaderOptionTypes.distortion,
            ShaderOptionTypes.soft_fade
        };

        private static List<AlbedoOptions> AlbedoOrder = new List<AlbedoOptions> 
        {
            AlbedoOptions.default_,
            AlbedoOptions.detail_blend,
            AlbedoOptions.constant_color,
            AlbedoOptions.two_change_color,
            AlbedoOptions.four_change_color,
            AlbedoOptions.three_detail_blend,
            AlbedoOptions.two_detail_overlay,
            AlbedoOptions.two_detail,
            AlbedoOptions.color_mask,
            AlbedoOptions.two_detail_black_point,
            AlbedoOptions.two_change_color_anim_overlay,
            AlbedoOptions.chameleon,
            AlbedoOptions.two_change_color_chameleon,
            AlbedoOptions.chameleon_masked,
            AlbedoOptions.color_mask_hard_light,
            AlbedoOptions.two_change_color_tex_overlay,
            AlbedoOptions.chameleon_albedo_masked,
            AlbedoOptions.custom_cube,
            AlbedoOptions.two_color,
            AlbedoOptions.scrolling_cube_mask,
            AlbedoOptions.scrolling_cube,
            AlbedoOptions.scrolling_texture_uv,
            AlbedoOptions.texture_from_misc,
            AlbedoOptions.four_change_color_applying_to_specular
        };

        private static List<BumpMappingOptions> BumpMappingOrder = new List<BumpMappingOptions> 
        {
            BumpMappingOptions.off,
            BumpMappingOptions.standard,
            BumpMappingOptions.detail,
            BumpMappingOptions.detail_masked,
            BumpMappingOptions.detail_plus_detail_masked,
            BumpMappingOptions.detail_plus_unorm, 
            BumpMappingOptions.standard_wrinkle, //Halo Reach
            BumpMappingOptions.detail_wrinkle //Halo Reach
        };

        private static List<AlphaTestOptions> AlphaTestOrder = new List<AlphaTestOptions> 
        {
            AlphaTestOptions.none,
            AlphaTestOptions.simple
        };

        private static List<SpecularMaskOptions> SpecularMaskOrder = new List<SpecularMaskOptions> 
        {
            SpecularMaskOptions.no_specular_mask,
            SpecularMaskOptions.specular_mask_from_diffuse,
            SpecularMaskOptions.specular_mask_mult_diffuse, //Halo Reach 
            SpecularMaskOptions.specular_mask_from_texture,
            SpecularMaskOptions.specular_mask_from_color_texture
        };

        private static List<MaterialModelOptions> MaterialModelOrder = new List<MaterialModelOptions> 
        {
            MaterialModelOptions.diffuse_only,
            MaterialModelOptions.cook_torrance,
            MaterialModelOptions.cook_torrance_custom_cube,
            MaterialModelOptions.cook_torrance_pbr_maps,
            MaterialModelOptions.cook_torrance_two_color_spec_tint,
            MaterialModelOptions.cook_torrance_scrolling_cube,
            MaterialModelOptions.cook_torrance_scrolling_cube_mask,
            MaterialModelOptions.cook_torrance_rim_fresnel,
            MaterialModelOptions.cook_torrance_from_albedo,
            MaterialModelOptions.cook_torrance_odst, //From Pedros shadergen code, leaving it here for now as an example of how to add new options outside of the scope of the offical shaders.
            MaterialModelOptions.two_lobe_phong,
            MaterialModelOptions.two_lobe_phong_tint_map,
            MaterialModelOptions.foliage,
            MaterialModelOptions.none,
            MaterialModelOptions.glass,
            MaterialModelOptions.organism,
            MaterialModelOptions.single_lobe_phong,
            MaterialModelOptions.car_paint,
            MaterialModelOptions.hair
        };

        private static List<EnvironmentMappingOptions> EnvrionmentMappingOrder = new List<EnvironmentMappingOptions> 
        {
            EnvironmentMappingOptions.none,
            EnvironmentMappingOptions.dynamic,
            EnvironmentMappingOptions.from_flat_texture,
            EnvironmentMappingOptions.per_pixel,
            EnvironmentMappingOptions.custom_map,
            EnvironmentMappingOptions.from_flat_exture_as_cubemap 
        };

        private static List<SelfIlluminationOptions> SelfIlluminationOrder = new List<SelfIlluminationOptions> 
        {
            SelfIlluminationOptions.off,
            SelfIlluminationOptions.simple,
            SelfIlluminationOptions.three_channel_self_illum,
            //SelfIlluminationOptions.3_channel_self_illum,
            SelfIlluminationOptions.plasma,
            SelfIlluminationOptions.from_diffuse,
            SelfIlluminationOptions.illum_detail,
            SelfIlluminationOptions.meter,
            SelfIlluminationOptions.self_illum_times_diffuse,
            SelfIlluminationOptions.simple_with_alpha_mask,
            SelfIlluminationOptions.simple_four_change_color,
            SelfIlluminationOptions.illum_detail_world_space_four_cc,
            SelfIlluminationOptions.multilayer_additive,
            SelfIlluminationOptions.paletized_plasma,
            SelfIlluminationOptions.illum_change_color,
            SelfIlluminationOptions.illum_change_color_detail
        };

        private static List<BlendModeOptions> BlendModeOrder = new List<BlendModeOptions> 
        {
            BlendModeOptions.opaque,
            BlendModeOptions.additive,
            BlendModeOptions.multiply,
            BlendModeOptions.double_multiply,
            BlendModeOptions.pre_multiplied_alpha,
            BlendModeOptions.alpha_blend
        };

        private static List<ParallaxOptions> ParallaxOrder = new List<ParallaxOptions> 
        {
            ParallaxOptions.off,
            ParallaxOptions.simple,
            ParallaxOptions.interpolated,
            ParallaxOptions.simple_detail
        };

        private static List<MiscOptions> MiscOrder = new List<MiscOptions> 
        {
            MiscOptions.first_person_never,
            MiscOptions.first_person_sometimes,
            MiscOptions.first_person_always,
            MiscOptions.first_person_never_with_rotating_bitmaps,
            MiscOptions.rotating_bitmaps_super_slow
        };

        private static List<WetnessOptions> WetnessOrder = new List<WetnessOptions> 
        {
            //WetnessOptions.default, //overlapping Indentifiers
            WetnessOptions.flood,
            WetnessOptions.proof,
            WetnessOptions.simple,
            WetnessOptions.ripples
        };

        private static List<DistortionOptions> DistortionOrder = new List<DistortionOptions> 
        {
            DistortionOptions.off,
            DistortionOptions.on
        };

        private static List<SoftFadeOptions> SoftFadeOrder = new List<SoftFadeOptions> 
        {
            SoftFadeOptions.off,
            SoftFadeOptions.on
        };

        private static List<AlphaBlendSourceOptions> AlphaBlendSourceOrder = new List<AlphaBlendSourceOptions> 
        {
            AlphaBlendSourceOptions.from_albedo_alpha_without_fresnel,
            AlphaBlendSourceOptions.from_albedo_alpha,
            AlphaBlendSourceOptions.from_opacity_map_alpha,
            AlphaBlendSourceOptions.from_opacity_map_rgb,
            AlphaBlendSourceOptions.from_opacity_map_alpha_and_albedo_alpha
        };

        private static List<OverlayOptions> OverlayOrder = new List<OverlayOptions>
        {
            OverlayOptions.overlay_map,
            OverlayOptions.overlay_map_scale_y,
            OverlayOptions.overlay_map_translation_y,
            OverlayOptions.overlay_map_filter_mode,
            OverlayOptions.overlay_tint,
            OverlayOptions.overlay_intensity
        };

        private static List<WarpOptions> WarpOrder = new List<WarpOptions>
        {
            WarpOptions.none,
            WarpOptions.from_texture,
            WarpOptions.parallax_simple
        };

        private static List<EdgeFadeOptions> EdgeFadeOrder = new List<EdgeFadeOptions>
        {
            EdgeFadeOptions.none,
            EdgeFadeOptions.simple
        };

        private static List<MiscAttrAnimationOptions> MiscAttrAnimationOrder = new List<MiscAttrAnimationOptions>
        {
            MiscAttrAnimationOptions.off,
            MiscAttrAnimationOptions.scrolling_cube,
            MiscAttrAnimationOptions.scrolling_projected
        };


        public int GetTypeCount() => 10;

        public int GetOptionCount(int typeIndex)
        {
            switch ((ShaderOptionTypes)(typeIndex))
            {
                case ShaderOptionTypes.albedo: return 15;
                case ShaderOptionTypes.bump_mapping: return 4;
                case ShaderOptionTypes.alpha_test: return 2;
                case ShaderOptionTypes.specular_mask: return 4;
                case ShaderOptionTypes.material_model: return 9;
                case ShaderOptionTypes.environment_mapping: return 5;
                case ShaderOptionTypes.self_illumination: return 10;
                case ShaderOptionTypes.blend_mode: return 6;
                case ShaderOptionTypes.parallax: return 4;
                case ShaderOptionTypes.misc: return 4;
                case ShaderOptionTypes.misc_attr_animation: return 3;
                case ShaderOptionTypes.distortion: return 2;
                case ShaderOptionTypes.warp: return 3;
                case ShaderOptionTypes.overlay: return 5;
                case ShaderOptionTypes.soft_fade: return 2;
                case ShaderOptionTypes.edge_fade: return 2;
                case ShaderOptionTypes.wetness_options: return 5;
                case ShaderOptionTypes.alpha_blend_source: return 5;
                default: return 0;
            }
        }

        public int GetTypeIndex(int typeIndex)
        {
            return TypeOrder.IndexOf((ShaderOptionTypes)typeIndex);
        }

        public int GetOptionIndex(int typeIndex, int optionIndex)
        {
            switch ((ShaderOptionTypes)(typeIndex))
            {
                case ShaderOptionTypes.albedo:                  return AlbedoOrder.IndexOf((AlbedoOptions)optionIndex);
                case ShaderOptionTypes.bump_mapping:            return BumpMappingOrder.IndexOf((BumpMappingOptions)optionIndex);
                case ShaderOptionTypes.alpha_test:              return AlphaTestOrder.IndexOf((AlphaTestOptions)optionIndex);
                case ShaderOptionTypes.specular_mask:           return SpecularMaskOrder.IndexOf((SpecularMaskOptions)optionIndex);
                case ShaderOptionTypes.material_model:          return MaterialModelOrder.IndexOf((MaterialModelOptions)optionIndex);
                case ShaderOptionTypes.environment_mapping:     return EnvrionmentMappingOrder.IndexOf((EnvironmentMappingOptions)optionIndex);
                case ShaderOptionTypes.self_illumination:       return SelfIlluminationOrder.IndexOf((SelfIlluminationOptions)optionIndex);
                case ShaderOptionTypes.blend_mode:              return BlendModeOrder.IndexOf((BlendModeOptions)optionIndex);
                case ShaderOptionTypes.parallax:                return ParallaxOrder.IndexOf((ParallaxOptions)optionIndex);
                case ShaderOptionTypes.misc:                    return MiscOrder.IndexOf((MiscOptions)optionIndex);
                case ShaderOptionTypes.misc_attr_animation:     return MiscAttrAnimationOrder.IndexOf((MiscAttrAnimationOptions)optionIndex);
                case ShaderOptionTypes.distortion:              return DistortionOrder.IndexOf((DistortionOptions)optionIndex);
                case ShaderOptionTypes.warp:                    return WarpOrder.IndexOf((WarpOptions)optionIndex);
                case ShaderOptionTypes.overlay:                 return OverlayOrder.IndexOf((OverlayOptions)optionIndex);
                case ShaderOptionTypes.soft_fade:               return SoftFadeOrder.IndexOf((SoftFadeOptions)optionIndex);
                case ShaderOptionTypes.edge_fade:               return EdgeFadeOrder.IndexOf((EdgeFadeOptions)optionIndex);
                case ShaderOptionTypes.wetness:                 return WetnessOrder.IndexOf((WetnessOptions)optionIndex);
                case ShaderOptionTypes.alpha_blend_source:      return AlphaBlendSourceOrder.IndexOf((AlphaBlendSourceOptions)optionIndex);
                default:                                        return 0;
            }
        }

        public string ToString(List<int> options)
        {
            if (options.Count < GetTypeCount())
                return "Invalid option count";

            string result = "";
            result += $"Albedo: {(AlbedoOptions)options[0]} \n";
            result += $"Bump Mapping: {(BumpMappingOptions)options[1]} \n";
            result += $"Alpha Test: {(AlphaTestOptions)options[2]} \n";
            result += $"Specular Mask: {(SpecularMaskOptions)options[3]} \n";
            result += $"Material Mode: {(MaterialModelOptions)options[4]} \n";
            result += $"Enviornment Mapping: {(EnvironmentMappingOptions)options[5]} \n";
            result += $"Self Illumination: {(SelfIlluminationOptions)options[6]} \n";
            result += $"Blend Mode: {(BlendModeOptions)options[7]} \n";
            result += $"Parallax: {(ParallaxOptions)options[8]} \n";
            result += $"Misc: {(MiscOptions)options[9]} \n";
            result += $"Misc Attr Animation: {(MiscOptions)options[10]} \n";
            result += $"Distortion: {(DistortionOptions)options[11]} \n";
            result += $"Soft Fade: {(SoftFadeOptions)options[12]} \n";
            result += $"Warp: {(WarpOptions)options[13]} \n";
            result += $"Overlay: {(OverlayOptions)options[14]} \n";
            result += $"Edge Fade: {(EdgeFadeOptions)options[15]} \n";
            result += $"Wetness: {(WetnessOptions)options[16]} \n";
            result += $"Alpha Blend Source: {(AlphaBlendSourceOptions)options[17]} \n";

            return result;
        }

        public void PrintOptions(List<int> options)
        {
            Console.WriteLine(ToString(options));
        }

        private enum ShaderOptionTypes
        {
            albedo = 0,
            bump_mapping = 1,
            alpha_test = 2,
            specular_mask = 3,
            material_model = 4,
            environment_mapping = 5,
            self_illumination = 6,
            blend_mode = 7,
            parallax = 8,
            misc = 9,
            misc_attr_animation = 10,
            distortion = 11,
            soft_fade = 12,
            warp = 13,
            overlay = 14,
            edge_fade = 15,
            wetness_options = 16,
            alpha_blend_source = 17,
            wetness = 18
        }

        private enum AlbedoOptions
        {
            default_,
            detail_blemd,
            constant_color,
            two_change_color,
            four_change_color,
            three_detail_blemd,
            two_detail_overlay,
            two_detail,
            color_mask,
            two_detail_black_point,
            two_change_color_anim_overlay,
            chameleon,
            two_change_color_chameleon,
            chameleon_masked,
            color_mask_hard_light,
            detail_blend,
            three_detail_blend,
            two_change_color_tex_overlay,
            chameleon_albedo_masked,
            custom_cube,
            two_color,
            scrolling_cube_mask,
            scrolling_cube,
            scrolling_texture_uv,
            texture_from_misc,
            four_change_color_applying_to_specular
        }

        private enum BumpMappingOptions
        {
            off,
            standard,
            detail,
            detail_masked,
            detail_plus_detail_masked,
            detail_plus_unorm,
            standard_wrinkle,
            detail_wrinkle
        }

        private enum AlphaTestOptions
        {
            none,
            simple
        }

        private enum SpecularMaskOptions
        {
            no_specular_mask,
            specular_mask_from_diffuse,
            specular_mask_from_texture,
            specular_mask_from_color_texture,
            specular_mask_mult_diffuse
        }

        private enum MaterialModelOptions
        {
            diffuse_only,
            cook_torrance,
            two_lobe_phong,
            foliage,
            none,
            glass,
            organism,
            single_lobe_phong,
            car_paint,
            hair, // Reach Shader
            cook_torrance_custom_cube,
            cook_torrance_pbr_maps, // H3 Shader
            cook_torrance_two_color_spec_tint,
            cook_torrance_scrolling_cube,
            cook_torrance_scrolling_cube_mask,
            cook_torrance_rim_fresnel,
            cook_torrance_from_albedo,
            two_lobe_phong_tint_map,
            cook_torrance_odst
        }

        private enum EnvironmentMappingOptions
        {
            none,
            per_pixel,
            dynamic,
            from_flat_texture,
            custom_map,
            from_flat_exture_as_cubemap
        }

        private enum SelfIlluminationOptions
        {
            off,
            simple,
            three_channel_self_illum,
            //3_channel_self_illum,
            plasma,
            from_diffuse,
            illum_detail,
            meter,
            self_illum_times_diffuse,
            simple_with_alpha_mask,
            simple_four_change_color,
            illum_detail_world_space_four_cc,
            illum_change_color,
            multilayer_additive,
            paletized_plasma,
            illum_change_color_detail
        }

        private enum BlendModeOptions
        {
            opaque,
            additive,
            multiply,
            alpha_blend,
            double_multiply,
            pre_multiplied_alpha
        }

        private enum ParallaxOptions
        {
            off,
            simple,
            interpolated,
            simple_detail
        }

        private enum MiscOptions
        {
            first_person_never,
            first_person_sometimes,
            first_person_always,
            first_person_never_with_rotating_bitmaps,
            rotating_bitmaps_super_slow
        }

        private enum MiscAttrAnimationOptions
        {
            off,
            scrolling_cube,
            scrolling_projected
        }

        private enum DistortionOptions
        {
            off,
            on
        }

        private enum SoftFadeOptions
        {
            off,
            on
        }

        private enum WetnessOptions
        {
            flood,
            proof,
            simple,
            ripples
        }

        private enum WarpOptions
        {
            none,
            from_texture,
            parallax_simple
        }

        private enum AlphaBlendSourceOptions
        {
            from_albedo_alpha,
            from_opacity_map_alpha,
            from_opacity_map_rgb,
            from_opacity_map_alpha_and_albedo_alpha,
            from_albedo_alpha_without_fresnel
        }

        private enum OverlayOptions
        {
            overlay_map,
            overlay_map_scale_y,
            overlay_map_scale_x, //Unsure if this flag exists
            overlay_map_translation_y,
            overlay_map_translation_x, // Same here
            overlay_map_filter_mode,
            overlay_tint,
            overlay_intensity
        }

        private enum EdgeFadeOptions
        {
            none,
            simple
        }
    }

    
}
