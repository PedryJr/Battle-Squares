using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{

    internal interface IRigidBody
    {
        public void OnDestroy();
        public Vector2 position { get; set; }
    }

    public class RigidBodyComponent : ComponentBase, IRigidBody
    {

        internal IRigidBody component;
        public RigidBodyComponent() => integrationType = IntegrationType.RigidBody;
        public override void OnDestroy() => component.OnDestroy();


        public Vector2 position { get => component.position; set => component.position = value; }

    }

}