using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace InputAssistance;

public class ConfigOptions : OptionInterface
{
    public ConfigOptions()
    {
        BackflipKey = this.config.Bind<KeyCode>("BackflipKey", KeyCode.A, new ConfigurableInfo("Immediately performs a backflip if possible: Briefly changes direction and jumps simultaneously.\nIf held down while in the air, triggers a roll if possible. Releasing while rolling jumps."));
        BackflipLength = this.config.Bind<int>("BackflipLength", 15, new ConfigAcceptableRange<int>(0, 80));
        SlideKey = this.config.Bind<KeyCode>("SlideKey", KeyCode.C, new ConfigurableInfo("Immediately performs a slide if possible. Jumps if released while in a slide.\nIf an object is thrown mid-slide, may attempt to perform an extended slide."));
        SlideLength = this.config.Bind<int>("SlideLength", 15, new ConfigAcceptableRange<int>(0, 80));
        AutoWallJump = this.config.Bind<bool>("AutoWallJump", true, new ConfigurableInfo("When jump is held, climbs up a ledge if possible.\nOtherwise, if not moving away from a wall, attempts to wall jump."));
        HoldToGrabPoles = this.config.Bind<bool>("HoldToGrabPoles", true, new ConfigurableInfo("If GRAB is held and the player is airborne, attempts to grab poles."));
        EasyExtSlide = this.config.Bind<bool>("EasyExtSlide", true);
    }

    //configs
    public readonly Configurable<KeyCode> BackflipKey;
    public readonly Configurable<int> BackflipLength;
    public readonly Configurable<KeyCode> SlideKey;
    public readonly Configurable<int> SlideLength;
    public readonly Configurable<bool> AutoWallJump;
    public readonly Configurable<bool> HoldToGrabPoles;
    public readonly Configurable<bool> EasyExtSlide;
    //public readonly Configurable<KeyCode> RollKey; //absorbed into Backflip

    public override void Initialize()
    {
        var opTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            opTab
        };

        const float l = 10f, //left margin
            w = 100f, //config width
            h = 25f, //config height sometimes
            t = l+l+w, //text start
            s = 30; //vertical spacing (I find a vertical spacing of 30 to be pleasant for configs; 25 is probably the minimum
        float y = 550f; //current height

        opTab.AddItems(
            new OpLabel(l, y, "Options", true),
            new OpLabel(t, y-=s+s, "Backflip or Roll"),
            new OpKeyBinder(BackflipKey, new(l, y), new(w, h)) { description = "Immediately performs a backflip if possible: Briefly changes direction and jumps simultaneously.\nIf held down while in the air, triggers a roll if possible. Releasing while rolling jumps." },
            new OpLabel(t+l, y -= s, "/40 Backflip Time"), //indented slightly
            new OpUpdown(BackflipLength, new (l+l, y), w) { description = "How long (in ticks = 1/40th of a second) a the backflip key should switch your direction." },
            new OpLabel(t, y -= s, "Slide"),
            new OpKeyBinder(SlideKey, new(l, y), new(w, h)) { description = "Immediately performs a slide if possible. Jumps if released while in a slide.\nIf an object is thrown mid-slide, may attempt to perform an extended slide." },
            new OpLabel(t + l, y -= s, "/40 Slide Time"), //indented slightly
            new OpUpdown(SlideLength, new(l + l, y), w) { description = "The minimum time (in ticks = 1/40th of a second) of a slide starting with the slide key." },
            new OpLabel(t, y -= s, "Auto Walljump"),
            new OpCheckBox(AutoWallJump, l, y) { description = "When jump is held, climbs up a ledge if possible.\nOtherwise, if not moving away from a wall, attempts to wall jump." },
            new OpLabel(t, y -= s, "Grab poles with GRAB"),
            new OpCheckBox(HoldToGrabPoles, l, y) { description = "If GRAB is held and the player is airborne, attempts to grab poles." },
            new OpLabel(t, y -= s, "Easy Extended Slide"),
            new OpCheckBox(EasyExtSlide, l, y) { description = "Makes it easier to perform an extended slide by automatically throwing backwards if throwing an object during a slide.\nONLY applies if neither left nor right is held down." }
        );
    }

}