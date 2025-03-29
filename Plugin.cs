using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using RWCustom;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace InputAssistance;

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.InputAssistance";
    public const string MOD_NAME = "Input Assistance";
    public const string MOD_VERSION = "0.0.1";

    //made static for easy access. Hopefully this mod should never be initiated twice anyway...
    public static ConfigOptions Options;
    #region setup
    public Plugin()
    {
        try
        {
            Options = new ConfigOptions();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void OnEnable()
    {
        Logger.LogDebug("OnEnable called!");

        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }
    private void OnDisable()
    {
        Logger.LogDebug("OnDisable called!");
        //Remove hooks
        On.RainWorld.OnModsInit -= RainWorld_OnModsInit;

        if (IsInit)
        {
            On.Player.checkInput -= Player_checkInput;

            IsInit = false;
        }
    }

    private bool IsInit;
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return; //prevents adding hooks twice

            Logger.LogDebug("OnModsInit called!");

            On.Player.checkInput += Player_checkInput;

            MachineConnector.SetRegisteredOI(MOD_ID, Options);

            IsInit = true;

            Logger.LogDebug("Hooks added!");
        }
        catch (Exception ex)
        {
            Custom.Logger.Error(ex.ToString());
            throw;
        }
    }
    #endregion

    #region hooks

    private static int backflipCounter = 0, backflipDir = 0, rollJumpCounter = 0, slideCounter = 0, slideDir = 0, slideJumpCounter = 0;
    private static bool rollDown = false, slideDown = false;

    private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        orig(self);

        if (self.playerState.playerNumber != 0) return; //only applies to Player1

        //Timers and counters and stuff
        if (backflipCounter > 0 && backflipDir != 0)
        {
            self.input[0].x = backflipDir;
            if (self.input[0].jmp) self.input[1].jmp = false; //try to allow jumping again if manually pressed
            self.input[0].jmp = true; //also jump; that's nice
        }
        if (rollJumpCounter > 0 || slideJumpCounter > 0)
        {
            if (self.input[0].jmp) self.input[1].jmp = false; //try to allow jumping again if manually pressed
            self.input[0].jmp = true;
        }
        if (slideCounter > 0)
        {
            self.input[0].x = slideDir;
            self.input[0].y = -1;
            self.input[0].downDiagonal = slideDir;
            self.input[0].jmp = true;
        }

        backflipCounter--;
        rollJumpCounter--;
        slideCounter--;
        slideJumpCounter--;

        //Backflip/roll button
        if (Input.GetKey(Options.BackflipKey.Value))
        {
            if (!rollDown)
            {
                //if player on ground and moving, try to backflip
                if (self.canJump > 0 && (self.input[0].x != 0 || self.flipDirection != 0 || self.slideDirection != 0))
                {
                    int dir = self.input[0].x == 0 ? (self.flipDirection == 0 ? self.slideDirection : self.flipDirection) : self.input[0].x;
                    self.input[0].x = -dir; //change direction
                    self.input[1].x = dir;
                    self.input[0].jmp = true; //jump!
                    self.input[1].jmp = false;

                    backflipDir = -dir;
                    backflipCounter = Options.BackflipLength.Value;
                    Logger.LogDebug("Backflip!");
                }
            }
            //trying to start rolling?
            else if (backflipCounter <= 0 && self.input[0].x != 0 && self.input[0].y < 1)
            {
                self.input[0].y = -1;
                self.input[0].downDiagonal = self.input[0].x;
            }

            if (self.canJump <= 0)
                self.input[0].jmp = true; //keep jump held while in a backflip

            rollDown = true;
        }
        else if (rollDown) //we are rolling; tried to stop rolling
        {
            if (self.rollCounter > 0)
            {
                self.input[0].jmp = true;
                rollJumpCounter = 15;
                Logger.LogDebug("Roll jump!");
            }
            rollDown = false;
        }

        //Slide button
        if (Input.GetKey(Options.SlideKey.Value))
        {
            if (!slideDown)
            {
                //if player on ground, try to slide
                if (self.bodyMode == Player.BodyModeIndex.Stand)
                {
                    slideDir = (self.input[0].x == 0) ? self.flipDirection : self.input[0].x;
                    self.input[0].x = slideDir;
                    self.input[0].y = -1;
                    self.input[0].downDiagonal = slideDir;
                    slideCounter = 4;
                    Logger.LogDebug("Slide!");
                }
            }
            else if (self.animation == Player.AnimationIndex.BellySlide)
            {
                //if already sliding, keep sliding
                self.input[0].x = slideDir;
                self.input[0].y = -1;
                self.input[0].downDiagonal = slideDir;
            }
            slideDown = true;
        }
        else if (slideDown) //we are sliding; tried to stop sliding
        {
            if (self.animation == Player.AnimationIndex.BellySlide)
            {
                self.input[0].jmp = true;
                slideJumpCounter = Options.SlideLength.Value;
                slideCounter = 0; //stop trying to maintain the slide; we're jumping out of it!
                Logger.LogDebug("Slide jump!");
            }
            slideDown = false;
        }

        //Auto-grab poles when falling and not trying to roll
        if (Options.HoldToGrabPoles.Value && self.input[0].pckp && self.canJump <= 0 && !rollDown
            && (self.animation == Player.AnimationIndex.None || self.animation == Player.AnimationIndex.Flip))
            self.input[0].y = 1; //hold up

        //Auto-slide on walls
        if (Options.AutoWallJump.Value)
        {
            if (self.bodyChunks[0].contactPoint.x != 0)
            {
                if (self.input[0].x == 0) self.input[0].x = self.bodyChunks[0].contactPoint.x; //hug walls
                if (self.input[0].jmp) self.input[1].jmp = false; //jump constantly
            }
        }
    }

    #endregion
}
