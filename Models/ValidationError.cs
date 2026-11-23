using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain_1.Models
{
    public enum ValidationError
    {
        None,                //помилок немає
        InvalidMerkleRoot,   //Merkle Root не відповідає транзакціям блоку
        InvalidHash,         //Hash не відповідає даним блоку
        BrokenPreviousHash,  //порушено зв'язок з попереднім блоком
        InvalidProofOfWork   //Hash не має потрібної кількості нулів
    }
}
