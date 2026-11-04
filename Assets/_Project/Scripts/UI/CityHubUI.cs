using System;
using DallasDash.Core;
using DallasDash.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DallasDash.UI
{
    public sealed class CityDirectionHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> Changed;
        public void OnPointerDown(PointerEventData e) => Changed?.Invoke(true);
        public void OnPointerUp(PointerEventData e) => Changed?.Invoke(false);
        public void OnPointerExit(PointerEventData e) => Changed?.Invoke(false);
        void OnDisable() => Changed?.Invoke(false);
    }

    public partial class MenuUI
    {
        GameObject cityHub;
        Text cityDestination, cityWallet, cityCaptions;
        RidersCityWorld cityWorld;
        Vector2 cityTarget;
        bool cityTravelling, cityLeft, cityRight, cityUp, cityDown;
        Button cityVisit;
        Transform cityRoot;
        Image cityFooter;

        // ---------- Free roam: Riders City inside the one Dash Riders menu
        // The menu and the city are one screen (client): FREE ROAM keeps the top bar and the five menu tiles,
        // swaps the logo and big buttons for steering and VISIT, and shows the city behind. Every destination
        // opens the same panel as its menu tile, so nothing in the city is duplicated. Single player only:
        // multiplayer, clubs and phrase chat were removed (client, 3 Nov).
        bool roaming;

        void BuildCityHub(Transform root)
        {
            cityRoot = root;
            cityWorld = new GameObject("Riders City world").AddComponent<RidersCityWorld>();
            cityWorld.transform.SetParent(transform, false);
            cityHub = new GameObject("Free roam HUD", typeof(RectTransform));
            cityHub.transform.SetParent(title.transform, false);
            var rect = (RectTransform)cityHub.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var p = cityHub.transform;

            var status = UiKit.Panel(p, "Roam status", new Vector2(0, 715), new Vector2(760, 110), UiKit.Navy);
            status.raycastTarget = false;
            UiKit.Label(status.transform, "RIDERS CITY  /  FREE ROAM", 34, new Vector2(0, 22), new Vector2(740, 50), Color.white);
            cityWallet = UiKit.Label(status.transform, "", 24, new Vector2(0, -24), new Vector2(740, 40), UiKit.Cream);
            UiKit.Button(p, "START DELIVERY", new Vector2(0, 585), new Vector2(620, 100), UiKit.Pink, Play, 40, "play");
            cityCaptions = UiKit.Label(p, "", 28, new Vector2(0, 470), new Vector2(940, 80), UiKit.Cream);

            cityFooter = UiKit.Panel(p, "Destination", new Vector2(0, -330), new Vector2(1000, 84), UiKit.Navy);
            cityFooter.raycastTarget = false;
            cityDestination = UiKit.Label(p, "", 30, new Vector2(0, -330), new Vector2(960, 70), Color.white);
            cityVisit = UiKit.Button(p, "VISIT", new Vector2(0, -435), new Vector2(620, 92), UiKit.Pink, VisitCityPlace, 34);

            Hold(p, "^", new Vector2(-320, -770), on => cityUp = on);
            Hold(p, "<", new Vector2(-430, -862), on => cityLeft = on);
            Hold(p, "v", new Vector2(-320, -862), on => cityDown = on);
            Hold(p, ">", new Vector2(-210, -862), on => cityRight = on);
            UiKit.Button(p, "BACK TO MENU", new Vector2(250, -815), new Vector2(470, 110), UiKit.Surface, ExitCity, 34);

            if (DashTour.Active)
                foreach (var button in cityHub.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
            cityHub.SetActive(false);
            roamButton = UiKit.Button(title.transform, "FREE ROAM", new Vector2(210, -425), new Vector2(405, 95), UiKit.Green, EnterCity, 32);
        }

        Button roamButton;

        void Hold(Transform parent, string text, Vector2 pos, Action<bool> action)
        {
            var button = UiKit.Button(parent, text, pos, new Vector2(100, 82), UiKit.Blue, () => { }, 36);
            button.gameObject.AddComponent<CityDirectionHold>().Changed = action;
        }

        /// Free roam on or off, inside the title screen: the top bar and the menu tiles never move away.
        void SetRoam(bool on)
        {
            roaming = on;
            foreach (Transform child in title.transform)
            {
                if (child.gameObject == cityHub) continue;
                if (titleKeep.Contains(child.gameObject))
                {
                    // The menu tiles lift above the steering pad while roaming.
                    var rt = (RectTransform)child;
                    if (titleTiles.Contains(child.gameObject)) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, on ? -590f : TileRowY);
                    continue;
                }
                child.gameObject.SetActive(!on);
            }
            cityHub.SetActive(on);
        }

        void EnterCity()
        {
            if (!cityHub || !GameManager.Instance || GameManager.Instance.State != RunState.Ready) return;
            cityWorld.Show(true);
            if (!cityWorld.Active)
            {
                Toast(cityWorld.AssetError.Length > 0 ? cityWorld.AssetError : "City model catalogue failed to load.");
                return;
            }
            title.SetActive(true);
            SetRoam(true);
            cityTravelling = false;
        }

        void ExitCity()
        {
            cityWorld.Input = Vector2.zero; cityWorld.Show(false);
            if (roaming) SetRoam(false);
            title.SetActive(GameManager.Instance.State == RunState.Ready);
        }

        bool AnyMenuPanelOpen => (crewPanel && crewPanel.activeSelf) || (shopPanel && shopPanel.activeSelf) || (missionsPanel && missionsPanel.activeSelf)
            || (ranksPanel && ranksPanel.activeSelf) || (daily && daily.activeSelf) || (districtPanel && districtPanel.activeSelf)
            || (rewardsPanel && rewardsPanel.activeSelf) || (settings && settings.activeSelf);

        void UpdateCityDirectory(float dt)
        {
            if (!cityHub || !cityHub.activeSelf) { if (cityWorld) cityWorld.Input = Vector2.zero; return; }
            if (!GameManager.Instance || GameManager.Instance.State != RunState.Ready) { ExitCity(); return; }
            var keyboard = Keyboard.current;
            bool modal = AnyMenuPanelOpen;
            Vector2 move = modal ? Vector2.zero : new Vector2((cityRight ? 1 : 0) - (cityLeft ? 1 : 0), (cityUp ? 1 : 0) - (cityDown ? 1 : 0));
            if (!modal && keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y--;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y++;
            }
            if (move.sqrMagnitude > 0f) cityTravelling = false;
            if (cityTravelling && !modal)
            {
                Vector2 delta = cityTarget - new Vector2(cityWorld.Position.x, cityWorld.Position.z);
                if (delta.magnitude < 1f) cityTravelling = false; else move = delta.normalized;
            }
            cityWorld.Input = DashReview.WalkthroughActive && DashReview.WalkingCity ? DashReview.WalkthroughInput : DashTour.Active ? DashTour.Input : Vector2.ClampMagnitude(move, 1);
            cityWallet.text = "BURGERS " + PlayerProfile.Burgers.ToString("N0") + "   /   " + DashRun.DistrictNames[DashRun.District];
            cityCaptions.text = DashReview.WalkthroughActive && DashReview.WalkingCity ? DashReview.WalkthroughCaption : DashTour.Active ? DashTour.Caption : "";
            cityVisit.interactable = cityWorld.CanVisit;
            int nearest = cityWorld.NearestDestination;
            float away = Vector3.Distance(cityWorld.Position, RidersCityWorld.Destinations[nearest]);
            cityDestination.text = cityWorld.CanVisit ? RidersCityWorld.DestinationNames[nearest] + "   /   TAP VISIT"
                : "NEAREST  /  " + RidersCityWorld.DestinationNames[nearest] + "   " + away.ToString("0") + " m";
            if (!modal && keyboard?.enterKey.wasPressedThisFrame == true) VisitCityPlace();
        }

        /// The four places open exactly the panels the menu tiles open; the city stays behind them.
        void VisitCityPlace()
        {
            if (!cityWorld.CanVisit) { Toast("Ride closer to a destination."); return; }
            cityWorld.Input = Vector2.zero;
            switch (cityWorld.NearestDestination)
            {
                case 0: ShowDistricts(); break;
                case 1: ShowCrew(); break;
                case 2: ShowShop(); break;
                default: ShowRewards(); break;
            }
        }
    }
}
