using Newtonsoft.Json.Linq;
using SwarmUI.Builtin_ComfyUIBackend;
using SwarmUI.Core;
using SwarmUI.Text2Image;

namespace SwarmSkipLayerGuidanceExtension;

public class SkipLayerGuidanceExtension : Extension
{
    public static T2IRegisteredParam<double> Scale, StartPercent, EndPercent, RescalingScale;

    public static T2IRegisteredParam<string> LayerTarget;

    public static T2IParamGroup SkipLayerGuidanceGroup;

    public override void OnInit()
    {
        SkipLayerGuidanceGroup = new("Skip Layer Guidance", Toggles: true, Open: false, IsAdvanced: true, Description: "Applies Skip Layer Guidance (SLG, SD3.5 logic trick) or Spatiotemporal Skip Guidance (STG, LTX-Video logic trick).");
        Scale = T2IParamTypes.Register<double>(new("[SLG] Scale", "[Skip Layer Guidance]\nScale for guidance (akin to CFG).\nFor SD3.5, 3 is recommended.\nFor LTX-Video, 1 is recommended.",
            "3", Min: 0, Max: 10, Step: 0.5, Group: SkipLayerGuidanceGroup, OrderPriority: 1, Examples: ["1", "2", "3", "5"]
            ));
        StartPercent = T2IParamTypes.Register<double>(new("[SLG] Start Percent", "[Skip Layer Guidance]\nHow far into the generation before SLG starts applying.\nAt 0, does nothing (ie does not delay, and SLG runs in full).\nFor SD3, 0.01 is recommended. For LTX-Video, 0 is recommended.",
            "0", IgnoreIf: "0", Min: 0, Max: 1, Step: 0.01, Group: SkipLayerGuidanceGroup, OrderPriority: 2, ViewType: ParamViewType.SLIDER, Examples: ["0", "0.01", "0.1"]
            ));
        EndPercent = T2IParamTypes.Register<double>(new("[SLG] End Percent", "[Skip Layer Guidance]\nHow far into the generation before SLG stops applying.\nAt 1, does nothing (ie does not end early, and SLG runs in full).\nFor SD3, 0.15 is recommended. For LTX-Video, 1 is recommended.",
            "1", IgnoreIf: "1", Min: 0, Max: 1, Step: 0.01, Group: SkipLayerGuidanceGroup, OrderPriority: 3, ViewType: ParamViewType.SLIDER, Examples: ["0.15", "0.5", "1"]
            ));
        RescalingScale = T2IParamTypes.Register<double>(new("[SLG] Rescaling Scale", "[Skip Layer Guidance]\nRescaling Scale, for STG logic.\nAt 0, Skip Layer Guidance is used. Above 0, STG is applied.\nFOR SD3.5, 0 is default.\nFor LTX-Video, 0.7 is default.",
            "0.0", Min: 0, Max: 1, Step: 0.01, Group: SkipLayerGuidanceGroup, OrderPriority: 4, Examples: ["0", "0.5", "0.7"], Toggleable: true
            ));
        LayerTarget = T2IParamTypes.Register<string>(new("[SLG] Layer Target", "[Skip Layer Guidance]\nWhich layers to apply SLG to, as a comma separated list.\nFor SD3.5, defaults to '7, 8, 9'. For LTX-Video, defaults to '14, 19'",
            "7, 8, 9", Group: SkipLayerGuidanceGroup, OrderPriority: 5, Toggleable: true
            ));
        WorkflowGenerator.AddModelGenStep(g =>
        {
            if (g.UserInput.TryGet(Scale, out double scale))
            {
                string target = g.UserInput.Get(LayerTarget, g.IsLTXV() ? "14, 19" : "7, 8, 9");
                string newNode = g.CreateNode("SkipLayerGuidanceDiT", new JObject()
                {
                    ["model"] = g.LoadingModel,
                    ["double_layers"] = target,
                    ["single_layers"] = target,
                    ["scale"] = scale,
                    ["start_percent"] = g.UserInput.Get(StartPercent, 0),
                    ["end_percent"] = g.UserInput.Get(EndPercent, 1),
                    ["rescaling_scale"] = g.UserInput.Get(RescalingScale, g.IsLTXV() ? 0.7 : 0)
                });
                g.LoadingModel = [newNode, 0];
            }
        }, -6.5);
    }
}
