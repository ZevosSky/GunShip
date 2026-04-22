namespace World
{

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TrailRenderer))]
public class TrailSeamSuppressor : MonoBehaviour
{
    private TrailRenderer _trail;
    private bool _resumeEnabled;
    private bool _resumeEmitting;
    private int _resumeFrame = -1;

    void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
    }

    void LateUpdate()
    {
        if (_trail == null || _resumeFrame < 0 || Time.frameCount < _resumeFrame)
            return;

        _trail.enabled = _resumeEnabled;
        _trail.emitting = _resumeEmitting;
        _resumeFrame = -1;
    }

    public void SuppressForSeamCrossing()
    {
        if (_trail == null)
            _trail = GetComponent<TrailRenderer>();

        if (_trail == null)
            return;

        _resumeEnabled = _trail.enabled;
        _resumeEmitting = _trail.emitting;
        _resumeFrame = Time.frameCount + 1;

        _trail.emitting = false;
        _trail.Clear();
        _trail.enabled = false;
    }

    void OnDisable()
    {
        _resumeFrame = -1;

        if (_trail != null)
            _trail.Clear();
    }
}

}
