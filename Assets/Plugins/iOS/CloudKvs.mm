// The iCloud key-value store behind the save on iOS (#39), called from ICloudKvsCloudSave.cs.
// One string under one key; Apple keeps the store at 1 MB in all, so the C# side measures before it sets.
// Strings cross as UTF-8 C strings; a string handed back must be given to VuonTraCloudKvs_Free.

#import <Foundation/Foundation.h>
#include <stdlib.h>
#include <string.h>

static id gVuonTraCloudKvsObserver = nil;
static volatile int gVuonTraCloudKvsChanged = 0;

static NSUbiquitousKeyValueStore *VuonTraCloudKvs_Store(void)
{
    return [NSUbiquitousKeyValueStore defaultStore];
}

static NSString *VuonTraCloudKvs_NSString(const char *text)
{
    return text == NULL ? @"" : [NSString stringWithUTF8String:text];
}

extern "C" {

// Listens for writes from the player's other devices and pulls the latest from iCloud. Safe to call more than once.
void VuonTraCloudKvs_Start(void)
{
    if (gVuonTraCloudKvsObserver != nil) return;
    gVuonTraCloudKvsObserver = [[NSNotificationCenter defaultCenter]
        addObserverForName:NSUbiquitousKeyValueStoreDidChangeExternallyNotification
                    object:VuonTraCloudKvs_Store()
                     queue:nil
                usingBlock:^(NSNotification *note) {
                    // Any reason counts (a server change, the initial sync, the store filling up, an account change):
                    // the C# side syncs and the sync itself decides what the copy up there means.
                    gVuonTraCloudKvsChanged = 1;
                }];
    [VuonTraCloudKvs_Store() synchronize];
}

// 1 when the device is signed in to an iCloud account; the store works only then.
int VuonTraCloudKvs_Available(void)
{
    return [[NSFileManager defaultManager] ubiquityIdentityToken] != nil ? 1 : 0;
}

// Sets the string under the key in the local copy of the store; 1 unless the store is unavailable.
int VuonTraCloudKvs_Set(const char *key, const char *value)
{
    if (key == NULL || value == NULL || !VuonTraCloudKvs_Available()) return 0;
    [VuonTraCloudKvs_Store() setString:VuonTraCloudKvs_NSString(value) forKey:VuonTraCloudKvs_NSString(key)];
    return 1;
}

// The string under the key as a malloc'd UTF-8 C string, or NULL when there is none. Free it with VuonTraCloudKvs_Free.
const char *VuonTraCloudKvs_Get(const char *key)
{
    if (key == NULL) return NULL;
    NSString *value = [VuonTraCloudKvs_Store() stringForKey:VuonTraCloudKvs_NSString(key)];
    if (value == nil) return NULL;
    const char *utf8 = [value UTF8String];
    return utf8 == NULL ? NULL : strdup(utf8);
}

void VuonTraCloudKvs_Free(const char *text)
{
    if (text != NULL) free((void *)text);
}

void VuonTraCloudKvs_Remove(const char *key)
{
    if (key == NULL) return;
    [VuonTraCloudKvs_Store() removeObjectForKey:VuonTraCloudKvs_NSString(key)];
}

// Pushes the local copy to iCloud; 1 when the store accepted it (the upload itself happens later, by iOS).
int VuonTraCloudKvs_Synchronize(void)
{
    return [VuonTraCloudKvs_Store() synchronize] ? 1 : 0;
}

// 1 once for every external change since the last call, then 0 again.
int VuonTraCloudKvs_TakeChanged(void)
{
    int changed = gVuonTraCloudKvsChanged;
    gVuonTraCloudKvsChanged = 0;
    return changed;
}

}
