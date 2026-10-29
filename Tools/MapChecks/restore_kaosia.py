"""Archived resource/automatic-placement migration command.
Geometry is now authored in saved scenes. This old writer is deliberately disabled
before it can overwrite scene edits or recreate removed runtime geometry data.
Historical implementation remains available in Git history.
"""
def main():
    raise SystemExit("이전 리소스/자동 배치 생성기는 사용 중지됐습니다. Unity 씬에서 타일을 편집하고 Tools/MapChecks/check_scene_tiles.py로 검증하세요.")
if __name__=="__main__":main()
