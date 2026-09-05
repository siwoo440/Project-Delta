using System.Collections.Generic; // UI 노드 목록
using UnityEngine; // Unity 기본 형식

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public enum DungeonMinimapRuntimeNodeKind // 미니맵 런타임 노드 종류
    {
        Root, // 최상위 노드
        Group, // 클리핑 그룹
        Texture, // 텍스처 표시
        Label // 텍스트 표시
    }

    public sealed class DungeonMinimapRuntimeNode // 미니맵 표시 노드
    {
        public DungeonMinimapRuntimeNodeKind Kind; // 노드 종류
        public Rect Rect; // 기존 GUI 좌표
        public Texture Texture; // 표시 텍스처
        public string Text = string.Empty; // 표시 문자열
        public Color Color = Color.white; // 표시 색상
        public int FontSize = 14; // 글자 크기
        public FontStyle FontStyle = FontStyle.Normal; // 글자 스타일
        public TextAnchor Alignment = TextAnchor.UpperLeft; // 글자 정렬
        public float RotationAngle; // 회전 각도
        public readonly List<DungeonMinimapRuntimeNode> Children =
            new List<DungeonMinimapRuntimeNode>(); // 자식 노드
    }

    public sealed class DungeonMinimapRuntimeFrame // 한 번의 미니맵 UI 프레임
    {
        public DungeonMinimapRuntimeNode Root; // 최상위 노드
    }

    public static class DungeonMinimapRuntimeGuiProxy // 기존 IMGUI를 UGUI 데이터로 기록
    {
        private static readonly Stack<DungeonMinimapRuntimeNode> GroupStack =
            new Stack<DungeonMinimapRuntimeNode>(); // 그룹 스택

        private static DungeonMinimapRuntimeFrame frame; // 현재 프레임
        private static GUISkin fallbackSkin; // 대체 GUI 스킨
        private static Matrix4x4 currentMatrix = Matrix4x4.identity; // 현재 GUI 행렬
        private static float rotationAngle; // 현재 회전 각도

        public static Color color = Color.white; // 기존 GUI.color 호환

        public static Matrix4x4 matrix // 기존 GUI.matrix 호환
        {
            get
            {
                return currentMatrix; // 현재 행렬 반환
            }
            set
            {
                currentMatrix = value; // 행렬 상태 저장

                if (value == Matrix4x4.identity) // 원본 행렬 복원 확인
                {
                    rotationAngle = 0f; // 회전 상태 초기화
                }
            }
        }

        public static GUISkin skin // 기존 GUI.skin 호환
        {
            get
            {
                if (fallbackSkin == null) // 대체 스킨 확인
                {
                    fallbackSkin = CreateFallbackSkin(); // 기본 스킨 생성
                }

                return fallbackSkin; // 현재 스킨 반환
            }
            set
            {
                fallbackSkin = value; // 외부 스킨 저장
            }
        }

        public static void BeginFrame() // 새 미니맵 프레임 시작
        {
            DungeonMinimapRuntimeNode root = new DungeonMinimapRuntimeNode(); // 최상위 노드 생성
            root.Kind = DungeonMinimapRuntimeNodeKind.Root; // 최상위 종류 지정
            root.Rect = new Rect(0f, 0f, Screen.width, Screen.height); // 전체 화면 좌표 지정

            frame = new DungeonMinimapRuntimeFrame(); // 새 프레임 생성
            frame.Root = root; // 최상위 노드 연결

            GroupStack.Clear(); // 이전 그룹 초기화
            GroupStack.Push(root); // 최상위 그룹 시작
            color = Color.white; // 기본 색상 초기화
            currentMatrix = Matrix4x4.identity; // 기본 행렬 초기화
            rotationAngle = 0f; // 기본 회전 초기화
        }

        public static DungeonMinimapRuntimeFrame EndFrame() // 현재 프레임 종료
        {
            if (frame == null) // 프레임 존재 확인
            {
                BeginFrame(); // 빈 프레임 생성
            }

            DungeonMinimapRuntimeFrame result = frame; // 현재 프레임 보관
            frame = null; // 프레임 상태 해제
            GroupStack.Clear(); // 그룹 스택 정리
            return result; // 완성 프레임 반환
        }

        public static void BeginGroup(Rect position) // 기존 GUI.BeginGroup 호환
        {
            EnsureFrame(); // 프레임 준비

            DungeonMinimapRuntimeNode group = new DungeonMinimapRuntimeNode(); // 그룹 노드 생성
            group.Kind = DungeonMinimapRuntimeNodeKind.Group; // 그룹 종류 지정
            group.Rect = position; // 그룹 좌표 저장
            CurrentParent().Children.Add(group); // 현재 부모에 그룹 추가
            GroupStack.Push(group); // 새 그룹 진입
        }

        public static void EndGroup() // 기존 GUI.EndGroup 호환
        {
            if (GroupStack.Count > 1) // 최상위 이외 그룹 확인
            {
                GroupStack.Pop(); // 현재 그룹 종료
            }
        }

        public static void DrawTexture(Rect position, Texture image) // 기존 GUI.DrawTexture 호환
        {
            AddTexture(position, image); // 텍스처 노드 추가
        }

        public static void DrawTexture(
            Rect position,
            Texture image,
            ScaleMode scaleMode) // 스케일 모드 오버로드
        {
            AddTexture(position, image); // 텍스처 노드 추가
        }

        public static void DrawTexture(
            Rect position,
            Texture image,
            ScaleMode scaleMode,
            bool alphaBlend) // 알파 오버로드
        {
            AddTexture(position, image); // 텍스처 노드 추가
        }

        public static void DrawTexture(
            Rect position,
            Texture image,
            ScaleMode scaleMode,
            bool alphaBlend,
            float imageAspect) // 비율 오버로드
        {
            AddTexture(position, image); // 텍스처 노드 추가
        }

        public static void Label(Rect position, string text) // 기본 라벨 표시
        {
            AddLabel(position, text, null); // 기본 스타일 라벨 추가
        }

        public static void Label(
            Rect position,
            string text,
            GUIStyle style) // 스타일 라벨 표시
        {
            AddLabel(position, text, style); // 스타일 라벨 추가
        }

        public static void Label(
            Rect position,
            GUIContent content) // GUIContent 라벨 표시
        {
            AddLabel(
                position,
                content != null
                    ? content.text
                    : string.Empty,
                null); // GUIContent 라벨 추가
        }

        public static void Label(
            Rect position,
            GUIContent content,
            GUIStyle style) // 스타일 GUIContent 라벨 표시
        {
            AddLabel(
                position,
                content != null
                    ? content.text
                    : string.Empty,
                style); // 스타일 GUIContent 라벨 추가
        }

        public static void RotateAroundPivot(
            float angle,
            Vector2 pivotPoint) // 기존 GUIUtility.RotateAroundPivot 호환
        {
            rotationAngle = angle; // 현재 회전 각도 저장
            currentMatrix = Matrix4x4.Rotate(
                Quaternion.Euler(0f, 0f, angle)); // 회전 행렬 상태 저장
        }

        private static void AddTexture(
            Rect position,
            Texture image) // 텍스처 노드 기록
        {
            EnsureFrame(); // 프레임 준비

            DungeonMinimapRuntimeNode node = new DungeonMinimapRuntimeNode(); // 텍스처 노드 생성
            node.Kind = DungeonMinimapRuntimeNodeKind.Texture; // 텍스처 종류 지정
            node.Rect = position; // 기존 좌표 저장
            node.Texture = image; // 텍스처 참조 저장
            node.Color = color; // 현재 GUI 색상 저장
            node.RotationAngle = rotationAngle; // 현재 회전 저장
            CurrentParent().Children.Add(node); // 현재 그룹에 노드 추가
        }

        private static void AddLabel(
            Rect position,
            string text,
            GUIStyle style) // 라벨 노드 기록
        {
            EnsureFrame(); // 프레임 준비

            DungeonMinimapRuntimeNode node = new DungeonMinimapRuntimeNode(); // 라벨 노드 생성
            node.Kind = DungeonMinimapRuntimeNodeKind.Label; // 라벨 종류 지정
            node.Rect = position; // 기존 좌표 저장
            node.Text = text ?? string.Empty; // 표시 문자열 저장
            node.Color = ResolveTextColor(style); // 스타일 글자색 저장
            node.FontSize = ResolveFontSize(style); // 스타일 글자 크기 저장
            node.FontStyle = style != null
                ? style.fontStyle
                : FontStyle.Normal; // 글자 스타일 저장
            node.Alignment = style != null
                ? style.alignment
                : TextAnchor.UpperLeft; // 글자 정렬 저장
            CurrentParent().Children.Add(node); // 현재 그룹에 노드 추가
        }

        private static Color ResolveTextColor(GUIStyle style) // 라벨 색상 계산
        {
            if (style != null
                && style.normal != null) // 스타일 상태 확인
            {
                Color styleColor = style.normal.textColor; // 스타일 글자색 읽기

                if (styleColor.a > 0f
                    || styleColor.r > 0f
                    || styleColor.g > 0f
                    || styleColor.b > 0f) // 명시 색상 확인
                {
                    return styleColor * color; // GUI 전체 색상 함께 적용
                }
            }

            return Color.white * color; // 기본 흰색 적용
        }

        private static int ResolveFontSize(GUIStyle style) // 라벨 글자 크기 계산
        {
            if (style != null
                && style.fontSize > 0) // 명시 글자 크기 확인
            {
                return style.fontSize; // 지정 크기 반환
            }

            return 14; // 기본 크기 반환
        }

        private static DungeonMinimapRuntimeNode CurrentParent() // 현재 그룹 조회
        {
            EnsureFrame(); // 프레임 준비
            return GroupStack.Peek(); // 현재 부모 반환
        }

        private static void EnsureFrame() // 프레임 존재 보장
        {
            if (frame == null
                || GroupStack.Count == 0) // 프레임 상태 확인
            {
                BeginFrame(); // 새 프레임 생성
            }
        }

        private static GUISkin CreateFallbackSkin() // OnGUI 외부 기본 스킨 생성
        {
            GUISkin result = ScriptableObject.CreateInstance<GUISkin>(); // 빈 스킨 생성
            result.label = new GUIStyle(); // 라벨 스타일 생성
            result.label.normal.textColor = Color.white; // 기본 글자색 지정
            return result; // 대체 스킨 반환
        }
    }
}
