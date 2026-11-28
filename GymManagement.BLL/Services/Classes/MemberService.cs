using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.Services.Classes;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Classes;
using GymManagement.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Service.Classes
{
    public class MemberService : IMemberService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IAttachmentService attachment;
        private readonly TimeProvider _timeProvider;

        public MemberService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAttachmentService attachment,
            TimeProvider? timeProvider = null)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.attachment = attachment;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<Result> CreateMemberAsync(
            CreateMemberViewModel model,
            AuditActor actor,
            CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<Member>();

            if (await repo.AnyAsync(m => m.Email == model.Email, ct))
                return Result.Fail("A member with this email already exists.");

            if (await repo.AnyAsync(m => m.Phone == model.Phone, ct))
                return Result.Fail("A member with this phone number already exists.");

            var photo = await attachment.UploadAsync(model.PhotoFile.OpenReadStream(), model.PhotoFile.FileName, "MembersPictures", ct);
            if (string.IsNullOrEmpty(photo))
                return Result.Validation("Profile photo upload failed (check file type and size).");

            var member = mapper.Map<Member>(model);
            member.Photo = photo;

            // The starting health record is part of the same aggregate and must use
            // the same audit path as every later create/update/delete.
            if (member.HealthRecord is not null)
            {
                var audit = CreateAudit(
                    HealthRecordAudit.Created,
                    actor,
                    member.Id,
                    member.HealthRecord);
                audit.MemberId = null;
                audit.Member = member;
                audit.HealthRecord = member.HealthRecord;
                unitOfWork.GetRepository<HealthRecordAudit>().Add(audit);
            }

            repo.Add(member);
            var result = await unitOfWork.SaveChangesAsync(ct);

            if (result == 0)
            {
                if (!string.IsNullOrEmpty(member.Photo))        
                    attachment.Delete(member.Photo, "MembersPictures");

                return Result.Fail("Failed To Create Member");
            }
            else

                return Result.Ok();
         }

        public async Task<PagedResult<MemberViewModel>> GetAllMembersAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var members = await unitOfWork.MemberRepository.GetMembersForListAsync(page, pageSize, ct);
            var totalCount = await unitOfWork.GetRepository<Member>().CountAsync(ct: ct);

            return new PagedResult<MemberViewModel>
            {
                Items = mapper.Map<List<MemberViewModel>>(members),
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalCount = totalCount },
            };
        }

        public async Task<string?> GetMemberPhotoAsync(int memberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().FirstOrDefaultAsync(
                m => m.Id == memberId, tracking: false, ct: ct);
            return member?.Photo;
        }

        public async Task<MemberViewModel?> GetMemberDetailsByIdAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct);
            if (member == null) return null;    

            var model = mapper.Map<Member , MemberViewModel>(member);

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var activeMembership = await unitOfWork.GetRepository<Membership>()
                .FirstOrDefaultAsync(x => x.MemberId == MemberId && x.EndDate >= now, ct: ct);

            if (activeMembership is not null)
            {
                var activePlan = await unitOfWork.GetRepository<Plan>().GetByIdAsync(activeMembership.PlanId, ct);

                model.PlanName = activePlan?.Name;
                model.MembershipStartDate = activeMembership.CreatedAt.ToShortDateString();
                model.MembershipEndDate   = activeMembership.EndDate.ToShortDateString();
            }
            return model;

            #region Load All Membership in Memory to get One Its Not Performance

            //var memberships = await membershipRepository.GetAllAsync();
            //var activeMembership = memberships.FirstOrDefault(m => m.MemberId == MemberId && m.IsActive); 
            #endregion
        }

        public async Task<HealthRecordViewModel?> GetMemberHealthRecordAsync(int MemberId, CancellationToken ct = default)
        {
            var record = await unitOfWork.GetRepository<HealthRecord>().FirstOrDefaultAsync(x => x.MemberId == MemberId, ct:ct);
            return record is null ? null : mapper.Map<HealthRecordViewModel>(record);

        }

        public async Task<Result> SaveMemberHealthRecordAsync(
            int memberId,
            HealthRecordViewModel model,
            AuditActor actor,
            CancellationToken ct = default)
        {
            var memberRepo = unitOfWork.GetRepository<Member>();
            if (!await memberRepo.AnyAsync(m => m.Id == memberId, ct))
                return Result.NotFound("Member not found.");

            var repo = unitOfWork.GetRepository<HealthRecord>();
            var record = await repo.FirstOrDefaultAsync(
                x => x.MemberId == memberId,
                tracking: true,
                ct: ct);
            var isNew = record is null;

            if (record is null)
            {
                record = mapper.Map<HealthRecord>(model);
                record.MemberId = memberId;
                record.CreatedAt = _timeProvider.GetUtcNow().UtcDateTime;
                repo.Add(record);
            }
            else
            {
                mapper.Map(model, record);
                record.MemberId = memberId;
                record.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
                repo.Update(record);
            }

            var audit = CreateAudit(
                isNew ? HealthRecordAudit.Created : HealthRecordAudit.Updated,
                actor,
                memberId,
                record);
            audit.HealthRecord = isNew ? record : null;
            audit.HealthRecordId = isNew ? null : record.Id;
            unitOfWork.GetRepository<HealthRecordAudit>().Add(audit);

            var saved = await unitOfWork.SaveChangesAsync(ct);
            return saved > 0 ? Result.Ok() : Result.Fail("Failed to save health record.");
        }

        public async Task<IReadOnlyList<HealthRecordAuditViewModel>> GetHealthRecordAuditAsync(
            int memberId,
            CancellationToken ct = default)
        {
            var audits = await unitOfWork.GetRepository<HealthRecordAudit>().GetLatestAsync(
                audit => audit.MemberId == memberId,
                take: 200,
                ct: ct);

            return mapper.Map<List<HealthRecordAuditViewModel>>(
                audits.OrderByDescending(audit => audit.CreatedAt).ToList());
        }

        public async Task<MemberToUpdateViewModel?> GetMemberToUpdateAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct);
            return member is null ? null :  mapper.Map<MemberToUpdateViewModel>(member);

        }

        public async Task<Result> RemoveMemberAsync(
            int MemberId,
            AuditActor actor,
            CancellationToken ct = default)
        {
            var memberRepo = unitOfWork.GetRepository<Member>();
            var member = await memberRepo.GetByIdAsync(MemberId, ct);
            if (member == null) return Result.NotFound("Member Not Found ");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var hasFutureBooking = await unitOfWork.BookingRepository.AnyAsync(
                b => b.MemberId == MemberId && b.Session.StartDate > now, ct);

            if (hasFutureBooking) 
                return Result.Fail("Cannot delete a member with upcoming sessions");

            var record = await unitOfWork.GetRepository<HealthRecord>().FirstOrDefaultAsync(
                x => x.MemberId == MemberId,
                tracking: false,
                ct: ct);

            var auditRepo = unitOfWork.GetRepository<HealthRecordAudit>();
            if (record is not null)
            {
                // The HealthRecord FK is left null: the record is about to be cascade-deleted.
                var audit = CreateAudit(HealthRecordAudit.Deleted, actor, MemberId, record);
                auditRepo.Add(audit);
            }

            // SQL Server disallows a cascade path straight from Members to this table,
            // so detach existing audit rows explicitly before deleting the member.
            var memberAudits = await auditRepo.GetAllAsync(
                audit => audit.MemberId == MemberId,
                tracking: true,
                ct: ct);
            foreach (var audit in memberAudits)
            {
                audit.MemberId = null;
                auditRepo.Update(audit);
            }

            memberRepo.Delete(member);
            var result = await unitOfWork.SaveChangesAsync(ct);

            if (result > 0)
            {
                if (!string.IsNullOrEmpty(member.Photo))
                    attachment.Delete(member.Photo, "MembersPictures");

                return Result.Ok();
            }
            return Result.Fail("Failed To Delete Member");
        }

        public async Task<Result> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<Member>();

            var member = await repo.GetByIdAsync(id, ct);
            if (member == null) return Result.Validation("Member Not Found ");

            if (await repo.AnyAsync(m => m.Email == model.Email && m.Id != id, ct))
                return Result.Fail("Another member is already using this email.");
            if (await repo.AnyAsync(m => m.Phone == model.Phone && m.Id != id, ct))
                return Result.Fail("Another member is already using this phone number.");

            mapper.Map(model, member);
            member.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            repo.Update(member);
            var result = await unitOfWork.SaveChangesAsync(ct);

            return result > 0 ? Result.Ok() : Result.Fail("Failed To update Membe");

        }

        private HealthRecordAudit CreateAudit(
            string action,
            AuditActor actor,
            int memberId,
            HealthRecord record) => new()
            {
                Action = action,
                ActorUserId = actor.UserId,
                ActorEmail = string.IsNullOrWhiteSpace(actor.Email) ? "unknown" : actor.Email,
                MemberId = memberId,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                Height = record.Height,
                Weight = record.Weight,
                BloodType = record.BloodType,
                Note = record.Note,
            };
    }
}    