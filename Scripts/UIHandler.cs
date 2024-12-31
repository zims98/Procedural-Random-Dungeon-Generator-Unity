using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    [Header("UI Sliders")]
    [SerializeField] Slider dungeonSizeSlider;
    [SerializeField] Slider constructionDelaySlider;
    [SerializeField] Slider hallwayChanceSlider;

    [Header("Slider Values (Text)")]
    [SerializeField] TextMeshProUGUI dungeonSizeText;
    [SerializeField] TextMeshProUGUI constructionDelayText;
    [SerializeField] TextMeshProUGUI hallwayChanceText;

    [Header("DungeonGenerator Reference")]
    [SerializeField] DungeonGenerator dungeonGenerator;

    // Events
    public UnityEvent<float> OnDungeonSizeChanged;
    public UnityEvent<float> OnConstructionDelayChanged;
    public UnityEvent<float> OnHallwayChanceChanged;

    void Start()
    {        
        dungeonSizeSlider.onValueChanged.AddListener(value => UpdateSliderValue(value, dungeonSizeText, OnDungeonSizeChanged));
        constructionDelaySlider.onValueChanged.AddListener(value => UpdateSliderValue(value, constructionDelayText, OnConstructionDelayChanged, "F2"));
        hallwayChanceSlider.onValueChanged.AddListener(value => UpdateSliderValue(value, hallwayChanceText, OnHallwayChanceChanged, "P1"));

        // Set the default values to the sliders. The default values are based on the values in DungeonGenerator
        InitializeSliders();
    }

    void UpdateSliderValue(float value, TextMeshProUGUI targetText, UnityEvent<float> sliderEvent, string format = null)
    {
        sliderEvent?.Invoke(value);

        // Apply formatting to the text if provided. If not, round to int
        targetText.text = format != null ? value.ToString(format) : Mathf.RoundToInt(value).ToString();
    }

    void InitializeSliders()
    {
        // The sliders are given the values from DungeonGenerator
        dungeonSizeSlider.value = DungeonGenerator.dungeonSize;
        constructionDelaySlider.value = DungeonGenerator.constructionDelay;
        hallwayChanceSlider.value = DungeonGenerator.hallwayChance;
    }
}
