using UnityEngine;

// このインターフェースを実装したコンポーネントは、KnockbackHandler経由で
// ノックバックの発生元・受け手として扱われる
public interface IKnockbackTarget
{
    // 衝突時の速度比較に使う、現在の移動スピード
    float CurrentSpeed { get; }

    // スタン（ノックバック硬直）が発生した瞬間に呼ばれる。
    // 各キャラ固有の状態リセット（徘徊フラグなど）はここで行う
    void OnKnockbackStunned(float duration);
}