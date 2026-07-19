using System;
using System.Diagnostics;

/// <summary>
/// 메서드, 프로퍼티등에서 다양한 맥락을 담아야 하는 요구사항이 있어서 Flags 어트리뷰트를 적용
/// </summary>
[Flags]
public enum AgentDocType
{
    CoreLogic = 1 << 0,     // 핵심 도메인 로직 (수정 시 파급 큼)
    Experimental = 1 << 1,  // 실험적 — 구조가 확정되지 않음
    Temporary = 1 << 2,     // 임시 처리 — 정식 구현으로 대체 예정
}

/// <summary>
/// AI 에이전트용 코드 문서화 어트리뷰트. tree-sitter 스캔으로 .llm-index/index.jsonl에 수집됨.
/// 이 어트리뷰트는 반드시 사람이 수동으로만 작성한다 — AI가 추가·수정·삭제해서는 안 된다.
/// 유일한 예외: 사용자가 /agent-doc-bootstrap 스킬을 직접 호출한 경우 AI의 신규 추가만 허용 (기존 수정·삭제는 여전히 금지).
/// </summary>
[Conditional("AGENT_DOC")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property)]
public sealed class AgentDocAttribute : Attribute
{
    public AgentDocType Type { get; }
    public string Purpose { get; }
    public string Warning { get; }

    public AgentDocAttribute(AgentDocType type, string purpose, string warning = null)
    {
        Type = type;
        Purpose = purpose;
        Warning = warning;
    }
}
