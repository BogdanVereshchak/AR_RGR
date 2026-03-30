using System.Collections;
using UnityEngine;

public class ObjectAnimation : MonoBehaviour {
    private Vector3 targetScale;
    void Start() {
        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
        StartCoroutine(Appear());
    }

    IEnumerator Appear() {
        float t = 0;
        float speed = 4.0f;
        while (t < 1) {
            t += Time.deltaTime * speed; 
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
        transform.localScale = targetScale;
    }
}