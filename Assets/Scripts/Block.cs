/*
 * Copyright 2002-2020 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace StudioMeowToon {
    /// <summary>
    /// ブロックの処理
    /// @author h.adachi
    /// </summary>
    public class Block : Common {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        int movementToX = 0; // 正の整数のみ

        [SerializeField]
        int movementToY = 0; // 正の整数のみ

        [SerializeField]
        int movementToZ = 0; // 正の整数のみ

        [SerializeField]
        float movementSpeed = 0.5f;

        [SerializeField]
        bool canPush; // 押すことが出来るかフラグ

        [SerializeField]
        int life = 1; // HP(耐久度)

        [SerializeField]
        bool canHold = false; // 持たれることが出来るかフラグ

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Vector3 origin;

        Vector3 toReach;

        bool positiveX = true;

        bool positiveY = true;

        bool positiveZ = true;

        GameObject playerObject; // プレイヤーオブジェクト

        GameObject itemObject; // アイテムオブジェクト

        bool isItemOnThis = false; // アイテムオブジェクトが上にのっているかフラグ

        bool isPlayerOnThis = false; // プレイヤーオブジェクトが上にのっているかフラグ

        bool isPushed; // 押されるフラグ

        float pushedCount; // 押されるカウント

        float pushedDistance; // 押された距離

        bool isGrounded; // 接地フラグ

        ExplodeParam explodeParam; // 破片生成用のパラメータクラス

        DoFixedUpdate doFixedUpdate; // FixedUpdate() メソッド用 フラグ

        Transform leftHandTransform; // Player 持たれる時の左手の位置 Transform

        Transform rightHandTransform; // Player 持たれる時の右手の位置 Transform

        // FIXME: 持たれる機能実装 TODO: _Item を付けなくても持てるように

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// 押すことが出来るかどうか。
        /// </summary>
        public bool pushable { get => canPush; }

        /// <summary>
        /// 押されるフラグをONする。
        /// </summary>
        public bool pushed { set => isPushed = value; }

        /// <summary>
        /// 次の一撃で破壊されるかどうか。
        /// </summary>
        public bool destroyable { get => life == 1 ? true : false; }

        /// <summary>
        /// プレイヤーに持たれているかどうか。
        /// </summary>
        public bool holdedByPlayer {
            get {
                if (transform.parent == null) {
                    return false;
                } else if (transform.parent.gameObject.IsPlayer()) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ロックオンシステム

        const string MAIN_CAMERA_TAG_NAME = "MainCamera";  // メインカメラに付いているタグ名

        // メインカメラに表示されているか
        bool isRendered = false;

        void OnBecameVisible() { // メインカメラに映った時
            if (Camera.current.tag == MAIN_CAMERA_TAG_NAME) {
                isRendered = true;
            }
        }

        void OnBecameInvisible() { // メインカメラに映らなくなった時
            if (Camera.current != null && Camera.current.tag == MAIN_CAMERA_TAG_NAME) {
                isRendered = false;
            }
        }

        // TODO: to a property
        public bool IsRenderedInMainCamera() { // メインカメラに写っているかどうか
            return isRendered;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        public void DestroyWithDebris(Transform bullet, int numberOfPiece = 8) { // 破片を発生させて消去する
            if (destroyable) { // 破壊可能の場合
                gameObject.GetTransformsInChildren().ToList().ForEach(_child => { // 全ての子を拾う
                    _child.SetParent(null); // 子オブジェクトを外す
                    if (_child.GetRigidbody() == null) {
                        _child.AddRigidbody();
                        _child.GetRigidbody().isKinematic = false;
                    }
                    if (_child != null && _child.LikeLadderBody()) { // ハシゴの場合
                        _child.GetCommon().autoDestroyAfter = 5.0f; // 5秒後に自動消去
                    }
                });
                explodeParam = ExplodeParam.getDefaultInstance(); // 破片生成パラメータ作成
                doFixedUpdate.explode = true; // 破片生成フラグON
            } else {
                explodeParam = ExplodeParam.getInstance(numberOfPiece / 2, 0.2f, 15); // 破片生成パラメータ作成
                doFixedUpdate.explode = true; // 破片生成フラグON
                shockedBy = bullet; // 弾で衝撃を受ける
            }
        }

        /// <summary>
        /// Player に下から衝撃を受ける。
        /// </summary>
        public void KnockedUp() {
            shockedBy = playerObject.transform;
        }

        // 持たれる実装用
        public Transform GetLeftHandTransform() { // キャラにIKで持たれる用
            return leftHandTransform;
        }

        public Transform GetRightHandTransform() { // キャラにIKで持たれる用
            return rightHandTransform;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        new void Awake() {
            base.Awake(); // 基底クラスのメソッド実行
            doFixedUpdate = DoFixedUpdate.getInstance(); // 物理挙動フラグ初期化
            isPushed = false;
            pushedCount = 0;
        }

        // Start is called before the first frame update.
        new void Start() {
            base.Start(); // 基底クラスのメソッド実行
            origin = transform.localPosition;
            toReach = new Vector3(origin.x + movementToX, origin.y + movementToY, origin.z + movementToZ);

            // 持たれる実装用
            if (canHold) {
                isGrounded = true; // false; MEMO: 初期設定の位置そのまま？
                // 持たれる時の手の位置オブジェクト初期化
                var _leftHandGameObject = new GameObject("LeftHand");
                var _rightHandGameObject = new GameObject("RightHand");
                leftHandTransform = _leftHandGameObject.transform;
                rightHandTransform = _rightHandGameObject.transform;
                leftHandTransform.parent = transform;
                rightHandTransform.parent = transform;
                leftHandTransform.localPosition = Vector3.zero;
                rightHandTransform.localPosition = Vector3.zero;
            }

            // Update is called once per frame.
            this.UpdateAsObservable()
                .Subscribe(_ => {
                    // 押される
                    if (canPush) { // 押されることが出来るブロックのみ
                        playerObject = GameObject.FindGameObjectWithTag("Player"); // MEMO:ここで設定しないと NullRef になる。なぜ？
                        if (isPushed && (pushedDistance < 1.0f)) { // 1ブロック分押すまで
                            bePushed(); // ブロックを押される
                            return;
                        } else if (isPushed && (pushedDistance >= 1.0f)) { // 1ブロック分押したら
                            align(); // 位置微調整
                            isPushed = false; // 押されるフラグOFF
                            pushedDistance = 0; // 押される距離リセット
                            playerObject.transform.parent = null; // 押してるプレイヤーの子オブジェクト化を解除
                            // TODO: 押せない時のアニメ？
                        }
                    }

                    // 自動移動
                    if (!gameObject.LikePiece()) { // 破片ではない場合
                        if (movementToX != 0 || movementToY != 0 || movementToZ != 0) { // TODO: canAutoMove 実装?
                            // FIXME: ブロックの上にアイテムを二つ置いて、また持ったらバグる
                            if (itemObject == null || itemObject.GetItem().holdedByPlayer) { // アイテムがプレイヤーに持たれた時
                                itemObject = null;
                                isItemOnThis = false;
                            }
                            moveAuto(); // 自動的に移動する
                        }
                    }
                });

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    if (doFixedUpdate.explode) { // 破片を飛散させる
                        gameObject.GetBoxCollider().enabled = false; // コライダー判定OFF※子に引き継がれる
                        explodePiece(explodeParam.number, explodeParam.scale, explodeParam.force);
                        gameObject.GetBoxCollider().enabled = true; // コライダー判定ON
                        doFixedUpdate.explode = false;
                        if (destroyable) {
                            Destroy(gameObject); // 自分を削除
                        }
                        life--; // HPを削る
                    }

                    // 持たれる実装用
                    if (!canHold) {
                        return;
                    } else {
                        if (isGrounded && transform.parent != null && transform.parent.gameObject.IsPlayer()) {
                            // 親が Player になった時
                            isGrounded = false; // 接地フラグOFF
                        } else if (!isGrounded && transform.parent != null && transform.parent.gameObject.IsPlayer()) {
                            // 親が Player 継続なら
                            if (!transform.parent.GetPlayer().Faceing) { // プレイヤーの移動・回転を待つ
                                    if (transform.parent.transform.position.y > transform.position.y + 0.2f) { // 0.2fは調整値
                                    beHolded(8.0f); // 上から持ち上げられる
                                } else {
                                    beHolded(); // 横から持ち上げられる
                                }
                            }
                        } else if (!isGrounded && (transform.parent == null || !transform.parent.gameObject.IsPlayer())) {
                            // 親が Player でなくなれば落下する
                            var _ray = new Ray(transform.position, new Vector3(0, -1f, 0)); // 下方サーチするレイ作成
                            if (Physics.Raycast(_ray, out RaycastHit _hit, 20f)) { // 下方にレイを投げて反応があった場合
#if DEBUG
                                Debug.DrawRay(_ray.origin, _ray.direction, Color.yellow, 3, false);
#endif
                                var _distance = (float) Math.Round(_hit.distance, 3, MidpointRounding.AwayFromZero);
                                if (_distance < 0.2f) { // ある程度距離が近くなら
                                    isGrounded = true; // 接地とする
                                    var _top = getHitTop(_hit.transform.gameObject); // その後、接地したとするオブジェクトのTOPを調べて
                                    transform.localPosition = Utils.ReplaceLocalPositionY(transform, _top); // その位置に置く
                                    align2(); // 位置調整
                                }
                            }
                            if (!isGrounded) { // まだ接地してなければ落下する
                                transform.localPosition -= new Vector3(0f, 5.0f * Time.deltaTime, 0f); // 5.0f は調整値
                            }
                        }
                    }
                });

            // プレイヤーが上に乗った
            this.OnCollisionEnterAsObservable().Where(x => x.IsPlayer())
                .Subscribe(x => {
                    if (isUnderBy(x)) {
                        playerObject = x.gameObject;
                        isPlayerOnThis = true;
                    }
                });

            // アイテムが上に乗った
            this.OnCollisionEnterAsObservable().Where(x => x.LikeItem())
                .Subscribe(x => {
                    if (isUnderBy(x)) {
                        itemObject = x.gameObject;
                        isItemOnThis = true;
                    }
                });

            // プレイヤーが上に乗っている
            this.OnCollisionStayAsObservable().Where(x => x.IsPlayer())
                 .Subscribe(x => {
                     if (isUnderBy(x)) {
                         playerObject = x.gameObject;
                         isPlayerOnThis = true;
                     }
                 });

            // アイテムが上に乗っている
            (this).OnCollisionStayAsObservable().Where(x => x.LikeItem())
                 .Subscribe(x => {
                     if (isUnderBy(x)) {
                         itemObject = x.gameObject;
                         isItemOnThis = true;
                     }
                 });

            // プレイヤーが上から離れた
            this.OnCollisionExitAsObservable().Where(x => x.IsPlayer())
                .Subscribe(_ => {
                    playerObject = null;
                    isPlayerOnThis = false;
                });

            // アイテムが上から離れた
            this.OnCollisionExitAsObservable().Where(x => x.LikeItem())
                .Subscribe(_ => {
                    itemObject = null;
                    isItemOnThis = false;
                });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// 接触対象より下かどうかを返す。
        /// </summary>
        bool isUnderBy(Collision x) {
            //  + 0.1f は誤差
            return Math.Round(getTop(), 2, MidpointRounding.AwayFromZero) < Math.Round(x.transform.position.y, 2, MidpointRounding.AwayFromZero) + 0.1f;
        }

        /// <summary>
        /// 破片を生成する。
        /// </summary>
        void explodePiece(int number = 8, float scale = 0.25f, int force = 15) {
            var _random = new System.Random(); // 乱数発生元
            var _min = -getRandomForce(force);
            var _max = getRandomForce(force);
            for (var i = 0; i < number; i++) { // 破片を生成する
                var _piece = Instantiate(gameObject); // 自分を複製する
                _piece.name += "_Piece"; // 破片には名前に "_Piece" を付加する
                _piece.transform.localScale = new Vector3(scale, scale, scale);
                if (_piece.GetRigidbody() == null) {
                    _piece.AddRigidbody();
                }
                _piece.GetRigidbody().isKinematic = false;
                var _v = new Vector3(_random.Next(_min, _max), _random.Next(_min, _max), _random.Next(_min, _max));
                _piece.GetRigidbody().AddForce(_v, ForceMode.Impulse);
                _piece.GetRigidbody().AddTorque(_v, ForceMode.Impulse);
                _piece.GetTransformsInChildren().ToList().ForEach(_child => {
                    if (_piece.name != _child.name) { // なぜか破片も破片の子リストにいるので除外
                        _child.parent = null;
                        Destroy(_child.gameObject); // 破片の子オブジェクトは最初に削除
                    }
                });
                _piece.GetBlock().autoDestroy = true; // 2秒後に破片を消去する
            }
        }

        /// <summary>
        /// 飛散する破片に加える力のランダム数値取得。
        /// </summary>
        int getRandomForce(int force) {
            var _random = new System.Random();
            return _random.Next((int) force / 2, (int) force * 2); // force の2分の1から2倍の範囲で
        }

        /// <summary>
        /// ブロックの位置をグリッドに合わせ微調整する。
        /// </summary>
        void align() {
            transform.position = new Vector3(
                (float) Math.Round(transform.position.x, 1, MidpointRounding.AwayFromZero),
                (float) Math.Round(transform.position.y, 1, MidpointRounding.AwayFromZero),
                (float) Math.Round(transform.position.z, 1, MidpointRounding.AwayFromZero)
            );
        }

        /// <summary>
        /// ブロックの位置をグリッドに合わせ微調整する。
        /// </summary>
        void align2() {
            transform.position = new Vector3(
                (float) Math.Round(transform.position.x * 2, 0, MidpointRounding.AwayFromZero) / 2, // 0.5単位にする為、2倍して2で割る
                (float) Math.Round(transform.position.y * 2, 0, MidpointRounding.AwayFromZero) / 2,
                (float) Math.Round(transform.position.z * 2, 0, MidpointRounding.AwayFromZero) / 2
            );
            transform.localRotation = new Quaternion(0, 0f, 0f, 0f);
        }

        /// <summary>
        /// 自身のY位置を取得する。
        /// </summary>
        float getTop() {
            float _height = gameObject.GetRenderer().bounds.size.y; // オブジェクトの高さ取得 
            float _y = transform.position.y; // オブジェクトのy座標取得(※0基点)
            float _top = _height + _y; // オブジェクトのTOP取得
            return _top;
        }

        /// <summary>
        /// 自動的に移動する。
        /// </summary>
        void moveAuto() {
            // FIXME: 自分の x,y,z の縮尺率を掛ける
            // FIXME: Half Block 対応
            float _sizeX = 0f, _sizeY = 0f, _sizeZ = 0f; // ブロックのサイズ
            if (movementToX != 0) { _sizeX = 1.0f; }
            if (movementToY != 0) { _sizeY = 1.0f; }
            if (movementToZ != 0) { _sizeZ = 1.0f; }
            // FIXME: 0.5 単位でしか Block を配置出来ない
            //X軸正方向
            if (positiveX) {
                if (Math.Round(transform.localPosition.x, 1, MidpointRounding.AwayFromZero) == toReach.x + (_sizeX / 2)) {
                    positiveX = false;
                } else if(transform.localPosition.x < toReach.x + (_sizeX / 2)) {
                    transform.localPosition += new Vector3(movementSpeed * Time.deltaTime, 0f, 0f);
                    if (isPlayerOnThis) {
                        playerObject.transform.localPosition += new Vector3(movementSpeed * Time.deltaTime, 0f, 0f);
                    }
                    if (isItemOnThis) {
                        itemObject.transform.localPosition += new Vector3(movementSpeed * Time.deltaTime, 0f, 0f);
                    }
                }
            // X軸負方向
            } else if (!positiveX) {
                if (Math.Round(transform.localPosition.x, 1, MidpointRounding.AwayFromZero) == origin.x) {
                    positiveX = true;
                } else if(transform.localPosition.x > origin.x) {
                    transform.localPosition -= new Vector3(movementSpeed * Time.deltaTime, 0f, 0f);
                    if (isPlayerOnThis) {
                        playerObject.transform.localPosition -= new Vector3(movementSpeed * Time.deltaTime, 0f, 0f);
                    }
                    if (isItemOnThis) {
                        itemObject.transform.localPosition -= new Vector3(movementSpeed * Time.deltaTime, 0f, 0f);
                    }
                }
            }
            // Y軸正方向
            if (positiveY) {
                if (Math.Round(transform.localPosition.y, 1, MidpointRounding.AwayFromZero) == toReach.y + (_sizeY / 2)) {
                    positiveY = false;
                } else if(transform.localPosition.y < toReach.y + (_sizeY / 2)) {
                        transform.localPosition += new Vector3(0f, movementSpeed * Time.deltaTime, 0f);
                    if (isPlayerOnThis) {
                        playerObject.transform.localPosition += new Vector3(0f, movementSpeed * Time.deltaTime, 0f);
                    }
                    if (isItemOnThis) {
                        itemObject.transform.localPosition += new Vector3(0f, movementSpeed * Time.deltaTime, 0f);
                    }
                }
            // Y軸負方向
            } else if (!positiveY) {
                if (Math.Round(transform.localPosition.y, 1, MidpointRounding.AwayFromZero) == origin.y) {
                    positiveY = true;
                } else if(transform.localPosition.y > origin.y) {
                    transform.localPosition -= new Vector3(0f, movementSpeed * Time.deltaTime, 0f);
                    if (isPlayerOnThis) {
                        playerObject.transform.localPosition -= new Vector3(0f, movementSpeed * Time.deltaTime, 0f);
                    }
                    if (isItemOnThis) {
                        itemObject.transform.localPosition -= new Vector3(0f, movementSpeed * Time.deltaTime, 0f);
                    }
                }
            }
            // Z軸正方向
            if (positiveZ) {
                if (Math.Round(transform.localPosition.z, 1, MidpointRounding.AwayFromZero) == toReach.z + (_sizeZ / 2)) {
                    positiveZ = false;
                } else if(transform.localPosition.z  < toReach.z + (_sizeZ / 2)) {
                    transform.localPosition += new Vector3(0f, 0f, movementSpeed * Time.deltaTime);
                    if (isPlayerOnThis) {
                        playerObject.transform.localPosition += new Vector3(0f, 0f, movementSpeed * Time.deltaTime);
                    }
                    if (isItemOnThis) {
                        itemObject.transform.localPosition += new Vector3(0f, 0f, movementSpeed * Time.deltaTime);
                    }
                }
            // Z軸負方向
            } else if (!positiveZ) {
                if (Math.Round(transform.localPosition.z, 1, MidpointRounding.AwayFromZero) == origin.z) {
                    positiveZ = true;
                } else if(transform.localPosition.z > origin.z) {
                    transform.localPosition -= new Vector3(0f, 0f, movementSpeed * Time.deltaTime);
                    if (isPlayerOnThis) {
                        playerObject.transform.localPosition -= new Vector3(0f, 0f, movementSpeed * Time.deltaTime);
                    }
                    if (isItemOnThis) {
                        itemObject.transform.localPosition -= new Vector3(0f, 0f, movementSpeed * Time.deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// ブロックが押される。
        /// </summary>
        void bePushed() {
            if (transform.name == playerObject.transform.parent.name) { // Player が自分の子オブジェクトなら押されている状況
                Ray _ray1 = new Ray(getPushedOriginVector3(transform, playerObject.transform.forward, 1), getPushedDirectionVector3(playerObject.transform.forward));
                if (Physics.Raycast(_ray1, out RaycastHit _hit1, 2f)) {
#if DEBUG
                    Debug.DrawRay(_ray1.origin, _ray1.direction * 2f, Color.cyan, 5, false);
#endif
                }
                float _distance1 = _hit1.distance;
                Ray _ray2 = new Ray(getPushedOriginVector3(transform, playerObject.transform.forward, 2), getPushedDirectionVector3(playerObject.transform.forward));
                if (Physics.Raycast(_ray2, out RaycastHit _hit2, 2f)) {
#if DEBUG
                    Debug.DrawRay(_ray2.origin, _ray2.direction * 2f, Color.cyan, 5, false);
#endif
                }
                float _distance2 = _hit2.distance;
                Ray _ray3 = new Ray(getPushedOriginVector3(transform, playerObject.transform.forward, 3), getPushedDirectionVector3(playerObject.transform.forward));
                if (Physics.Raycast(_ray3, out RaycastHit _hit3, 2f)) {
#if DEBUG
                    Debug.DrawRay(_ray3.origin, _ray3.direction * 2f, Color.cyan, 5, false);
#endif
                }
                float _distance3 = _hit3.distance;
                Ray _ray4 = new Ray(getPushedOriginVector3(transform, playerObject.transform.forward, 4), getPushedDirectionVector3(playerObject.transform.forward));
                if (Physics.Raycast(_ray4, out RaycastHit _hit4, 2f)) {
#if DEBUG
                    Debug.DrawRay(_ray4.origin, _ray4.direction * 2f, Color.cyan, 5, false);
#endif
                }
                float _distance4 = _hit4.distance;
                if ((_distance1 == 0f || _distance1 >= 0.5f) && (_distance2 == 0f || _distance2 >= 0.5f) && (_distance3 == 0f || _distance3 >= 0.5f) && (_distance4 == 0f || _distance4 >= 0.5f)) { // 距離が0か0.5以上なら
                    transform.Translate(
                        (float) Math.Round(playerObject.transform.forward.x) * Time.deltaTime,
                        0,
                        (float) Math.Round(playerObject.transform.forward.z) * Time.deltaTime
                    );
                    pushedDistance += Time.deltaTime; // 押した距離を足していく
                } else {
                    pushedDistance = 1.0f; // 押す限界なのでフラグを立てる
                }
            }
        }

        /// <summary>
        /// Player に押された方向を列挙体で返す。
        /// </summary>
        PushedDirection getPushedDirection(Vector3 forwardVector) {
            var _fX = (float) Math.Round(forwardVector.x);
            var _fY = (float) Math.Round(forwardVector.y);
            var _fZ = (float) Math.Round(forwardVector.z);
            if (_fX == 0 && _fZ == 1) { // Z軸正方向
                return PushedDirection.PositiveZ;
            }
            if (_fX == 0 && _fZ == -1) { // Z軸負方向
                return PushedDirection.NegativeZ;
            }
            if (_fX == 1 && _fZ == 0) { // X軸正方向
                return PushedDirection.PositiveX;
            }
            if (_fX == -1 && _fZ == 0) { // X軸負方向
                return PushedDirection.NegativeX;
            }
            // ここに来たら二軸の差を判定する TODO: ロジック再確認
            float _abX = Math.Abs(forwardVector.x);
            float _abZ = Math.Abs(forwardVector.z);
            if (_abX > _abZ) {
                if (_fX == 1) { // X軸正方向
                    return PushedDirection.PositiveX;
                }
                if (_fX == -1) { // X軸負方向
                    return PushedDirection.NegativeX;
                }
            } else if (_abX < _abZ) {
                if (_fZ == 1) { // Z軸正方向
                    return PushedDirection.PositiveZ;
                }
                if (_fZ == -1) { // Z軸負方向
                    return PushedDirection.NegativeZ;
                }
            }
            return PushedDirection.None; // 判定不明
        }

        /// <summary>
        /// Player に押された方向のベクトル値返す。
        /// </summary>
        Vector3 getPushedDirectionVector3(Vector3 forwardVector) {
            return new Vector3((float) Math.Round(forwardVector.x), 0, (float) Math.Round(forwardVector.z));
        }

        /// <summary>
        /// 押されるブロックが前方をサーチする時のRayポイント(※4箇所)を取得する。
        /// </summary>
        Vector3 getPushedOriginVector3(Transform pushed, Vector3 forwardVector, int idx) {
            var _OFFSET = 0.48f;
            if (getPushedDirection(forwardVector) == PushedDirection.PositiveZ) { // Z軸正方向
                if (idx == 1) {
                    return new Vector3(pushed.position.x + _OFFSET, pushed.position.y + (_OFFSET * 2), pushed.position.z);
                } else if (idx == 2) {
                    return new Vector3(pushed.position.x - _OFFSET, pushed.position.y + (_OFFSET * 2), pushed.position.z);
                } else if (idx == 3) {
                    return new Vector3(pushed.position.x + _OFFSET, pushed.position.y, pushed.position.z);
                } else if (idx == 4) {
                    return new Vector3(pushed.position.x - _OFFSET, pushed.position.y, pushed.position.z);
                }
            } else if (getPushedDirection(forwardVector) == PushedDirection.NegativeZ) { // Z軸負方向
                if (idx == 1) {
                    return new Vector3(pushed.position.x - _OFFSET, pushed.position.y + (_OFFSET * 2), pushed.position.z);
                } else if (idx == 2) {
                    return new Vector3(pushed.position.x + _OFFSET, pushed.position.y + (_OFFSET * 2), pushed.position.z);
                } else if (idx == 3) {
                    return new Vector3(pushed.position.x - _OFFSET, pushed.position.y, pushed.position.z);
                } else if (idx == 4) {
                    return new Vector3(pushed.position.x + _OFFSET, pushed.position.y, pushed.position.z);
                }
            } else if (getPushedDirection(forwardVector) == PushedDirection.PositiveX) { // X軸正方向
                if (idx == 1) {
                    return new Vector3(pushed.position.x, pushed.position.y + (_OFFSET * 2), pushed.position.z - _OFFSET);
                } else if (idx == 2) {
                    return new Vector3(pushed.position.x, pushed.position.y + (_OFFSET * 2), pushed.position.z + _OFFSET);
                } else if (idx == 3) {
                    return new Vector3(pushed.position.x, pushed.position.y, pushed.position.z - _OFFSET);
                } else if (idx == 4) {
                    return new Vector3(pushed.position.x, pushed.position.y, pushed.position.z + _OFFSET);
                }
            } else if (getPushedDirection(forwardVector) == PushedDirection.NegativeX) { // X軸負方向
                if (idx == 1) {
                    return new Vector3(pushed.position.x, pushed.position.y + (_OFFSET * 2), pushed.position.z + _OFFSET);
                } else if (idx == 2) {
                    return new Vector3(pushed.position.x, pushed.position.y + (_OFFSET * 2), pushed.position.z - _OFFSET);
                } else if (idx == 3) {
                    return new Vector3(pushed.position.x, pushed.position.y, pushed.position.z + _OFFSET);
                } else if (idx == 4) {
                    return new Vector3(pushed.position.x, pushed.position.y, pushed.position.z + _OFFSET);
                }
            }
            return new Vector3(0f, 0f, 0f); // TODO: 修正
        }

        // 衝突したオブジェクトの側面に当たったか判定する
        float getHitTop(GameObject hit) {
            float _height = hit.GetRenderer().bounds.size.y; // 対象オブジェクトの高さ取得 
            float _y = hit.transform.position.y; // 対象オブジェクトのy座標取得(※0基点)
            float _top = _height + _y; // 対象オブジェクトのTOP取得
            return _top;
        }

        /// <summary>
        /// プレイヤーに持ち上げられる。
        /// </summary>
        void beHolded(float speed = 2.0f) {
            if (transform.localPosition.y < 0.6f) { // 親に持ち上げられた位置に移動する: 0.6fは調整値
                var _direction = getPushedDirection(transform.parent.forward);
                if (_direction == PushedDirection.PositiveZ) { // Z軸正方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x,
                        transform.position.y + speed * Time.deltaTime, // 調整値
                        transform.parent.transform.position.z + 0.8f // 調整値
                    );
                    transform.rotation = Quaternion.Euler(-15f, 0f, 0f); // 15度傾ける
                } else if (_direction == PushedDirection.NegativeZ) { // Z軸負方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x,
                        transform.position.y + speed * Time.deltaTime,
                        transform.parent.transform.position.z - 0.8f
                    );
                    transform.rotation = Quaternion.Euler(15f, 0f, 0f);
                } else if (_direction == PushedDirection.PositiveX) { // X軸正方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x + 0.8f,
                        transform.position.y + speed * Time.deltaTime,
                        transform.parent.transform.position.z
                    );
                    transform.rotation = Quaternion.Euler(0f, 0f, 15f);
                } else if (_direction == PushedDirection.NegativeX) { // X軸負方向
                    transform.position = new Vector3(
                        transform.parent.transform.position.x - 0.8f,
                        transform.position.y + speed * Time.deltaTime,
                        transform.parent.transform.position.z
                    );
                    transform.rotation = Quaternion.Euler(0f, 0f, -15f);
                }
            }
        }

        #region DoFixedUpdate

        /// <summary>
        /// FixedUpdate() メソッド用のフラグクラス。
        /// </summary>
        protected class DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            bool _explode;

            public bool explode { get => _explode; set => _explode = value; }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// 初期化済みのインスタンスを返す。
            /// </summary>
            public static DoFixedUpdate getInstance() {
                var _instance = new DoFixedUpdate();
                _instance.ResetMotion();
                return _instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            /// <summary>
            /// 全フィールドの初期化
            /// </summary>
            public void ResetMotion() {
                _explode = false;
            }
        }

        #endregion

        #region ExplodeParam

        /// <summary>
        /// 破片生成用のパラメータークラス。
        /// </summary>
        protected class ExplodeParam {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields

            int _number; // 破片の数

            float _scale; // 破片の拡縮値

            int _force; // 破片飛散時に加える力

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            ExplodeParam(int number, float scale, int force) {
                this._number = number;
                this._scale = scale;
                this._force = force;
            }

            public static ExplodeParam getDefaultInstance() {
                return new ExplodeParam(8, 0.25f, 15); // デフォルト値
            }

            public static ExplodeParam getInstance(int number, float scale, int force) {
                return new ExplodeParam(number, scale, force);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public int number { get => _number; }

            public float scale { get => _scale; }

            public int force { get => _force; }

        }

        #endregion

    }

}
