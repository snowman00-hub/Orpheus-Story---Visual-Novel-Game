using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// VisualEvent에 붙여서 실행할 수 있는 화면 연출 효과의 공통 인터페이스다.
public interface IVisualEffect
{
    UniTask Play(VisualEffectContext context, CancellationToken cancellationToken);
}

// VisualEvent 효과가 씬 오브젝트에 접근할 때 사용하는 공통 컨텍스트다.
public class VisualEffectContext
{
    public VisualEffectContext(Canvas rootCanvas)
    {
        RootCanvas = rootCanvas;
    }

    public Canvas RootCanvas { get; }
}

// ScriptableObject로 등록 가능한 VisualEffect 기본 클래스다.
public abstract class VisualEffect : ScriptableObject, IVisualEffect
{
    public abstract UniTask Play(VisualEffectContext context, CancellationToken cancellationToken);
}
