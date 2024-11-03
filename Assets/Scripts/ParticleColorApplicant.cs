using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ParticleColorApplicant : MonoBehaviour
{

    [SerializeField]
    ParticleSystem[] emitters;


    
    public void Applycolor(PlayerBehaviour player)
    {

        foreach (ParticleSystem emitter in emitters)
        {

            MainModule mainModule = emitter.main;
            mainModule.startColor = player.playerDarkerColor;

            emitter.Emit(1);

        }

    }

}
