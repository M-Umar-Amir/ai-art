/**
 * KFC Dallas Dash — the phone app. A full-screen WebView running the game
 * from ONE bundled, self-contained page (assets/game/index.html, built by
 * ../dallas-dash/tools/build-mobile.mjs), so it plays fully offline and the
 * game code — including the replay rules the coupon server checks — is the
 * exact same code as the web version.
 *
 * The shell adds what a browser can't: haptics, keeping the screen awake,
 * pausing when the app goes to the background, and the system back button.
 */
import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, AppState, BackHandler, Platform, StyleSheet, Text, View } from "react-native";
import { WebView, type WebViewMessageEvent } from "react-native-webview";
import { Asset } from "expo-asset";
import * as Haptics from "expo-haptics";
import { useKeepAwake } from "expo-keep-awake";
import * as SplashScreen from "expo-splash-screen";
import { StatusBar } from "expo-status-bar";

SplashScreen.preventAutoHideAsync().catch(() => {});

const KFC_RED = "#c8102e";

export default function App() {
  useKeepAwake();
  const web = useRef<WebView>(null);
  const [uri, setUri] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // copy the bundled page out of the app package to a readable file
  useEffect(() => {
    (async () => {
      try {
        const page = Asset.fromModule(require("./assets/game/index.html"));
        await page.downloadAsync();
        setUri(page.localUri || page.uri);
      } catch (e) {
        setError(String((e as Error).message || e));
      }
    })();
  }, []);

  // background the app → pause the run (the page also listens for visibilitychange)
  useEffect(() => {
    const sub = AppState.addEventListener("change", (s) => {
      if (s !== "active") web.current?.injectJavaScript("window.__ddPause && window.__ddPause(); true;");
    });
    return () => sub.remove();
  }, []);

  // Android back: close an open sheet / pause, rather than leaving the app mid-run
  useEffect(() => {
    if (Platform.OS !== "android") return;
    const sub = BackHandler.addEventListener("hardwareBackPress", () => {
      web.current?.injectJavaScript(`(function(){
        var x = document.querySelector('.sheet:not(.hidden) [data-close]');
        if (x) { x.click(); } else if (window.__ddPause) { window.__ddPause(); }
      })(); true;`);
      return true;
    });
    return () => sub.remove();
  }, []);

  const onMessage = useCallback((e: WebViewMessageEvent) => {
    let msg: { haptic?: string };
    try {
      msg = JSON.parse(e.nativeEvent.data);
    } catch {
      return;
    }
    switch (msg.haptic) {
      case "heavy":
        Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error).catch(() => {});
        break;
      case "warn":
        Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium).catch(() => {});
        break;
      case "success":
        Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success).catch(() => {});
        break;
      case "light":
        Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light).catch(() => {});
        break;
    }
  }, []);

  if (error) {
    return (
      <View style={[styles.fill, styles.center]}>
        <Text style={styles.err}>Couldn't open the game: {error}</Text>
      </View>
    );
  }
  return (
    <View style={styles.fill}>
      <StatusBar hidden />
      {uri ? (
        <WebView
          ref={web}
          source={{ uri }}
          style={styles.fill}
          originWhitelist={["*"]}
          allowFileAccess
          allowFileAccessFromFileURLs
          allowUniversalAccessFromFileURLs
          allowingReadAccessToURL={uri.replace(/[^/]+$/, "")}
          javaScriptEnabled
          domStorageEnabled
          mediaPlaybackRequiresUserAction={false}
          allowsInlineMediaPlayback
          bounces={false}
          overScrollMode="never"
          scrollEnabled={false}
          setBuiltInZoomControls={false}
          textZoom={100}
          webviewDebuggingEnabled={__DEV__}
          onMessage={onMessage}
          onLoadEnd={() => SplashScreen.hideAsync().catch(() => {})}
          renderLoading={() => (
            <View style={[StyleSheet.absoluteFill, styles.center]}>
              <ActivityIndicator color="#fff" size="large" />
            </View>
          )}
          startInLoadingState
        />
      ) : (
        <View style={[styles.fill, styles.center]}>
          <ActivityIndicator color="#fff" size="large" />
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  fill: { flex: 1, backgroundColor: KFC_RED },
  center: { alignItems: "center", justifyContent: "center", padding: 24 },
  err: { color: "#fff", fontSize: 16, textAlign: "center" },
});
