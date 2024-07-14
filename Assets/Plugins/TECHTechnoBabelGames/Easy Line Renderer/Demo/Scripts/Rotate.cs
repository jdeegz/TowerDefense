using UnityEngine;

namespace TechnoBabelGames
{
    public class Rotate : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(0.05f, 0.1f, 0.02f, Space.Self);
        }
    }
}
