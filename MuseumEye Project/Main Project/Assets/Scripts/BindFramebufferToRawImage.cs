
using UnityEngine;
public class BindFramebufferToRawImage : MonoBehaviour {
	public MoviePlayer moviePlayer;
	public UnityEngine.UI.RawImage rawImage;

	void OnEnable () { moviePlayer.OnPlay += HandleOnPlay; }
	void OnDisable() { moviePlayer.OnPlay -= HandleOnPlay; }

	void HandleOnPlay (MoviePlayerBase caller) {
		rawImage.texture = moviePlayer.framebuffer;
	}
}
