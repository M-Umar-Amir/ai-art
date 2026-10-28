// Light / medium / heavy impact taps for Food Inc Game (Core/Haptics.cs). UIKit's impact generator
// is the system haptic for taps like these; it does nothing on devices without a Taptic Engine.
#import <UIKit/UIKit.h>

extern "C" void FoodIncHapticImpact(int style)
{
    UIImpactFeedbackStyle s = style == 0 ? UIImpactFeedbackStyleLight
                            : style == 1 ? UIImpactFeedbackStyleMedium
                                         : UIImpactFeedbackStyleHeavy;
    dispatch_async(dispatch_get_main_queue(), ^{
        UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
        [g prepare];
        [g impactOccurred];
    });
}
