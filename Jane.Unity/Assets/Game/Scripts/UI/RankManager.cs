using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankManager : MonoBehaviour
{
    public static RankManager instance;
    public StandingsGenerator standingsGenerator;

    private Dictionary<string, NetworkPlayer> players = new();
    private Ulid playerID;
    private List<KeyValuePair<string, NetworkPlayer>> sortedList = new();
    private bool isUpdating = false;
    private float switchTime = 0.15f;

    private void Awake() => instance = this;

    void Update()
    {
        if (!isUpdating)
        {
            SetPlayers();
        }
    }

    void SetPlayers()
    {
        IOrderedEnumerable<KeyValuePair<string, NetworkPlayer>> sortedPlayer = players.Where(x => !x.Value.IsFinished)
                                                                                      .OrderByDescending(x => x.Value.activeCheckpointIndex)
                                                                                      .ThenBy(x => x.Value.distanceToCheckpoint);
        
        List<KeyValuePair<string, NetworkPlayer>> tempList = sortedPlayer.ToList();

        if (sortedList.Any() && !sortedList.SequenceEqual(tempList) && tempList.Count == sortedList.Count)
        {
            List<int?> differentPositions = sortedList.Zip(tempList, (x, y) => x.Equals(y) ? (int?)null : Array.IndexOf(tempList.ToArray(), x)).ToList();
            differentPositions = differentPositions.Where(x => x != null).ToList();
            int change1 = differentPositions.First() ?? 0;
            int change2 = differentPositions.Last() ?? 0;
            int i = 0;

            foreach (KeyValuePair<string, NetworkPlayer> item in sortedPlayer)
            {
                if (i == change1)
                {
                    isUpdating = true;
                    GameObject tempBox = standingsGenerator.standingsBox[change1];
                    standingsGenerator.standingsBox[change1] = standingsGenerator.standingsBox[change2];
                    standingsGenerator.standingsBox[change2] = tempBox;

                    RectTransform first = standingsGenerator.standingsBox[change1].GetComponent<RectTransform>();
                    RectTransform second = tempBox.GetComponent<RectTransform>();
                    StartCoroutine(MoveStandings(first, second, sortedPlayer, switchTime));
                }
                i++;
            }
        }
        else if (!sortedList.Any())
        {
            int i = 0;
            foreach (KeyValuePair<string, NetworkPlayer> item in sortedPlayer)
            {
                if (item.Value.UniqueId == playerID)
                {
                    standingsGenerator.standingsBox[i].GetComponent<Image>().color = new Color(1.0f, 0.5f, 0f, 1.0f);
                }
                else
                {
                    standingsGenerator.standingsBox[i].GetComponent<Image>().color = new Color(0f, 0f, 0f, 1.0f);
                }
                standingsGenerator.standingsBox[i].GetComponentInChildren<TMP_Text>().text = " " + (i + 1) + "   " + item.Key;
                i++;
            }
        }
        sortedList = tempList;
    }
    
    IEnumerator MoveStandings(RectTransform rectTransformA, RectTransform rectTransformB, IOrderedEnumerable<KeyValuePair<string, NetworkPlayer>> sorted, float duration)
    {
        Vector2 startPosA = rectTransformA.anchoredPosition;
        Vector2 endPosA = rectTransformB.anchoredPosition;
        Vector2 startPosB = rectTransformB.anchoredPosition;
        Vector2 endPosB = rectTransformA.anchoredPosition;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);
            rectTransformA.anchoredPosition = Vector2.Lerp(startPosA, endPosA, t);
            rectTransformB.anchoredPosition = Vector2.Lerp(startPosB, endPosB, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        int i = 0;
        foreach (KeyValuePair<string, NetworkPlayer> item in sorted)
        {
            if (item.Value.UniqueId == playerID)
            {
                standingsGenerator.standingsBox[i].GetComponent<Image>().color = new Color(1.0f, 0.5f, 0f, 1.0f);
            }
            else
            {
                standingsGenerator.standingsBox[i].GetComponent<Image>().color = new Color(0f, 0f, 0f, 1.0f);
            }
            standingsGenerator.standingsBox[i].GetComponentInChildren<TMP_Text>().text = " " + (i + 1) + "   " + item.Key;
            i++;
        }

        isUpdating = false;
        rectTransformA.anchoredPosition = endPosA;
        rectTransformB.anchoredPosition = endPosB;
    }
}
