using MingDynasty.OpenToolkit.Core;
using MingDynasty.OpenToolkit.Simulation;
using UnityEngine;

namespace MingDynasty.OpenToolkit.Samples
{
    public sealed class BasicSimulationExample : MonoBehaviour
    {
        private IGameTimeService time;

        private void Awake()
        {
            IGameEventBus events = new GameEventBus();
            events.Subscribe<GameTimeAdvancedEvent>(OnTimeAdvanced);

            time = new GameTimeService(
                new GameClock(),
                new GenericCalendar(new CalendarDefinition()),
                events,
                null,
                secondsPerGameHour: 1.0d);
        }

        private void Update()
        {
            time.AdvanceRealSeconds(Time.deltaTime);
        }

        private void OnTimeAdvanced(GameTimeAdvancedEvent value)
        {
            if (value.Current.Minute == 0)
            {
                Debug.Log("Simulation time: " + value.Current);
            }
        }
    }
}
