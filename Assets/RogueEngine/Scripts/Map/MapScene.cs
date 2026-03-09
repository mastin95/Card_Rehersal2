using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueEngine.UI;

namespace RogueEngine
{

    public class MapScene : MonoBehaviour
    {
        private bool ended = false;

        void Awake()
        {

        }

        private void Start()
        {
            GameManager.Get().onConnectedGame += OnConnectGame;
            GameManager.Get().Connect();

            if (GameManager.Get().IsReady())
            {
                World world = GameManager.Get().GetWorld();
                world.Save(); //Auto save each time go back to map scene
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Get() != null)
                GameManager.Get().onConnectedGame -= OnConnectGame;
        }

        private void OnConnectGame()
        {
            if (!GameManager.Get().IsGameStarted())
            {
                //Start in test mode, scene loaded directly
                GameManager.Get().NewScenario(GameplayData.Get().test_scenario, "test");
                GameManager.Get().CreateChampion(GameplayData.Get().test_champion, 2);
                GameManager.Get().StartTest(WorldState.Map);
            }
        }

        void Update()
        {
            if (!GameManager.Get().IsReady())
                return;

            SwitchScene();
        }

        private void SwitchScene()
        {
            //Battle
            World world = GameManager.Get().GetWorld();
            if (world.state == WorldState.Battle)
            {
                MapData map = MapData.Get(world.map_id);
                EventBattle battle = EventBattle.Get(world.event_id);
                string scene = !string.IsNullOrEmpty(battle.scene) ? battle.scene : map.battle_scene;
                FadeToScene(scene);
            }

            //Next Map, reload map
            MapViewer viewer = MapViewer.Get();
            if (world.state == WorldState.Map && viewer.HasChanged())
            {
                MapData map = MapData.Get(world.map_id);
                FadeToScene(map.map_scene);
            }

            //End Game
            if (!ended && world.state == WorldState.Ended)
            {
                ended = true;
                EndGame();
            }
        }

        private void EndGame()
        {
            //Unlock rewards and show score panel
            ProgressManager.Get().UnlockNewRewards(2, 1);
            GameOverPanel.Get().Show();

            //Delete save file
            World world = GameManager.Get().GetWorld();
            World.Delete(world.filename);
        }

        public void FadeToScene(string scene)
        {
            StartCoroutine(RunFade(scene));
        }

        public IEnumerator RunFade(string scene)
        {
            yield return new WaitForSeconds(0.5f);
            BlackPanel.Get().Show();
            yield return new WaitForSeconds(1f);
            SceneNav.GoTo(scene);
        }
    }
}
